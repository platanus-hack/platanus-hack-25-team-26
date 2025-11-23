import base64
import os
from typing import TypedDict, Literal

from langchain_core.messages import HumanMessage
from langgraph.graph import StateGraph, END
from langgraph.types import Command
from langchain.chat_models import init_chat_model
from pydantic import BaseModel, Field

from prompts import EMAIL_PHISHING_PROMPT, SOCIAL_ENGINEERING_PROMPT

# API key configuration - read from environment (do NOT hardcode secrets)
api_key = os.environ.get("OPENAI_API_KEY")

# Graph State Definition
class GraphState(TypedDict):
    image_url: str
    image_type: str
    scoring: int
    reason: str


# Router Node: Classifies the image type
async def router_node(state: GraphState) -> Command[Literal["email_analyzer", "whatsapp_analyzer"]]:
    """
    Classifies whether the image is an email or WhatsApp conversation.
    Routes to the appropriate analyzer node.
    """
    class ImageTypeResponse(BaseModel):
        type: Literal["email", "whatsapp"] = Field(
            description="The type of image: 'email' for email screenshots, 'whatsapp' for WhatsApp conversations"
        )

    model = init_chat_model(
        model_provider="openai",
        model="gpt-5-mini-2025-08-07",
        api_key=api_key
    )
    structured_model = model.with_structured_output(ImageTypeResponse)

    message = HumanMessage(
        content=[
            {
                "type": "text",
                "text": "Analiza la imagen y determina si es una captura de pantalla de un EMAIL o una conversación de WHATSAPP. Responde solo con 'email' o 'whatsapp'."
            },
            {
                "type": "image_url",
                "image_url": {"url": state["image_url"]}
            }
        ]
    )

    response = await structured_model.ainvoke([message])

    # Determine next node based on image type
    next_node = "email_analyzer" if response.type == "email" else "whatsapp_analyzer"

    return Command(
        goto=next_node,
        update={"image_type": response.type}
    )


# Email Analyzer Node: Analyzes phishing in emails
async def email_analyzer_node(state: GraphState) -> Command[Literal[END]]:
    """
    Analyzes email screenshots for phishing indicators.
    Returns scoring and reason.
    """
    class PhishingResponse(BaseModel):
        scoring: int = Field(
            description="The phishing score from 1-10",
            ge=1,
            le=10
        )
        reason: str = Field(
            description="Brief explanation of the phishing assessment in Spanish (max 15 words)"
        )

    model = init_chat_model(
        model_provider="openai",
        model="gpt-5.1-2025-11-13",
        api_key=api_key
    )
    structured_model = model.with_structured_output(PhishingResponse)

    message = HumanMessage(
        content=[
            {
                "type": "text",
                "text": EMAIL_PHISHING_PROMPT
            },
            {
                "type": "image_url",
                "image_url": {"url": state["image_url"]}
            }
        ]
    )

    response = await structured_model.ainvoke([message])

    return Command(
        goto=END,
        update={
            "scoring": response.scoring,
            "reason": response.reason
        }
    )


# WhatsApp Analyzer Node: Analyzes social engineering in WhatsApp
async def whatsapp_analyzer_node(state: GraphState) -> Command[Literal[END]]:
    """
    Analyzes WhatsApp conversation screenshots for social engineering techniques.
    Returns scoring and reason.
    """
    class SocialEngineeringResponse(BaseModel):
        scoring: int = Field(
            description="The social engineering risk score from 1-10",
            ge=1,
            le=10
        )
        reason: str = Field(
            description="Brief explanation of the social engineering assessment in Spanish (max 15 words)"
        )

    model = init_chat_model(
        model_provider="openai",
        model="gpt-5.1-2025-11-13",
        api_key=api_key
    )
    structured_model = model.with_structured_output(SocialEngineeringResponse)

    message = HumanMessage(
        content=[
            {
                "type": "text",
                "text": SOCIAL_ENGINEERING_PROMPT
            },
            {
                "type": "image_url",
                "image_url": {"url": state["image_url"]}
            }
        ]
    )

    response = await structured_model.ainvoke([message])

    return Command(
        goto=END,
        update={
            "scoring": response.scoring,
            "reason": response.reason
        }
    )


# Build the Graph
def create_analysis_graph():
    """
    Creates and compiles the LangGraph for image analysis.

    Flow:
    1. router_node: Classifies image as email or whatsapp
    2. email_analyzer_node OR whatsapp_analyzer_node: Analyzes the image
    3. END: Returns final result
    """
    graph_builder = StateGraph(GraphState)

    # Add nodes
    graph_builder.add_node("router", router_node)
    graph_builder.add_node("email_analyzer", email_analyzer_node)
    graph_builder.add_node("whatsapp_analyzer", whatsapp_analyzer_node)

    # Set entry point
    graph_builder.set_entry_point("router")

    # Compile and return
    return graph_builder.compile()


# Create the compiled graph instance
analysis_graph = create_analysis_graph()


# Helper function to analyze an image
async def analyze_image(image_data: bytes, image_format: str = "png") -> dict:
    """
    Analyzes an image using the LangGraph.

    Args:
        image_data: Raw image bytes
        image_format: Image format (png, jpg, jpeg, etc.)

    Returns:
        dict with keys: image_type, scoring, reason
    """
    # Encode image to base64
    image_base64 = base64.b64encode(image_data).decode('utf-8')
    image_url = f"data:image/{image_format};base64,{image_base64}"

    # Initialize state
    initial_state = {
        "image_url": image_url,
        "image_type": "",
        "scoring": 0,
        "reason": ""
    }

    # Run the graph asynchronously
    result = await analysis_graph.ainvoke(initial_state)

    return {
        "image_type": result["image_type"],
        "scoring": result["scoring"],
        "reason": result["reason"]
    }
