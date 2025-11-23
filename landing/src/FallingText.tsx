import { useRef, useState, useEffect } from 'react';
import Matter from 'matter-js';
import './index.css';

interface FallingTextProps {
  text?: string;
  highlightWords?: string[];
  highlightClass?: string;
  trigger?: 'auto' | 'scroll' | 'click' | 'hover';
  backgroundColor?: string;
  wireframes?: boolean;
  gravity?: number;
  mouseConstraintStiffness?: number;
  fontSize?: string;
}

const FallingText: React.FC<FallingTextProps> = ({
  text = '',
  highlightWords = [],
  highlightClass = 'highlighted',
  trigger = 'auto',
  backgroundColor = 'transparent',
  wireframes = false,
  gravity = 1,
  mouseConstraintStiffness = 0.2,
  fontSize = '1rem'
}) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const textRef = useRef<HTMLDivElement | null>(null);
  const canvasContainerRef = useRef<HTMLDivElement | null>(null);

  const [effectStarted, setEffectStarted] = useState(false);

  // Mostrar texto normal inicialmente
  useEffect(() => {
    if (!textRef.current || !containerRef.current) return;
    if (!effectStarted) {
      // Esperar un frame para que el contenedor tenga sus dimensiones
      requestAnimationFrame(() => {
        if (!textRef.current || !containerRef.current) return;
        
        // Obtener padding del contenedor
        const computedStyle = window.getComputedStyle(containerRef.current);
        const paddingTop = parseFloat(computedStyle.paddingTop) || 0;
        const paddingRight = parseFloat(computedStyle.paddingRight) || 0;
        const paddingBottom = parseFloat(computedStyle.paddingBottom) || 0;
        const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;
        
        // Mostrar texto normal cuando el efecto no ha empezado, respetando padding
        const words = text.split(' ');
        const newHTML = words
          .map((word) => {
            const isHighlighted = highlightWords.some(hw => word.startsWith(hw));
            return `<span class="word ${isHighlighted ? highlightClass : ''}" style="display: inline-block; margin-right: 0.25em;">${word}</span>`;
          })
          .join(' ');
        textRef.current.innerHTML = newHTML;
        textRef.current.style.position = 'absolute';
        textRef.current.style.top = `${paddingTop}px`;
        textRef.current.style.left = `${paddingLeft}px`;
        textRef.current.style.width = `calc(100% - ${paddingLeft + paddingRight}px)`;
        textRef.current.style.height = `calc(100% - ${paddingTop + paddingBottom}px)`;
        textRef.current.style.display = 'block';
      });
    }
  }, [text, highlightWords, highlightClass, effectStarted]);

  useEffect(() => {
    if (trigger === 'auto') {
      setEffectStarted(true);
      return;
    }
    if (trigger === 'scroll' && containerRef.current) {
      const observer = new IntersectionObserver(
        ([entry]) => {
          if (entry.isIntersecting) {
            setEffectStarted(true);
            observer.disconnect();
          }
        },
        { threshold: 0.1 }
      );
      observer.observe(containerRef.current);
      return () => observer.disconnect();
    }
  }, [trigger]);


  useEffect(() => {
    if (!effectStarted) return;

    const { Engine, Render, World, Bodies, Runner, Mouse, MouseConstraint } = Matter;

    if (!containerRef.current || !canvasContainerRef.current || !textRef.current) return;

    let cleanup: (() => void) | null = null;
    
    // Esperar un frame para que el DOM se actualice con las palabras separadas
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (!containerRef.current || !canvasContainerRef.current || !textRef.current) return;
        
        const containerRect = containerRef.current.getBoundingClientRect();
        const width = containerRect.width || containerRef.current.offsetWidth || 800;
        const height = containerRect.height || containerRef.current.offsetHeight || 400;

        if (width <= 0 || height <= 0) {
          console.warn('FallingText: Container has no dimensions');
          return;
        }

        // Obtener el padding del contenedor
        const computedStyle = window.getComputedStyle(containerRef.current);
        const paddingTop = parseFloat(computedStyle.paddingTop) || 0;
        const paddingRight = parseFloat(computedStyle.paddingRight) || 0;
        const paddingBottom = parseFloat(computedStyle.paddingBottom) || 0;
        const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;

        // Área disponible dentro del padding
        const availableWidth = width - paddingLeft - paddingRight;
        const availableHeight = height - paddingTop - paddingBottom;
        const wallThickness = 50;

        const engine = Engine.create();
        engine.world.gravity.y = gravity;

        const render = Render.create({
          element: canvasContainerRef.current,
          engine,
          options: {
            width,
            height,
            background: backgroundColor,
            wireframes
          }
        });

        const boundaryOptions = {
          isStatic: true,
          render: { fillStyle: 'transparent' }
        };
        
        // Ajustar las paredes para que estén exactamente en los límites del área con padding
        const floorY = paddingTop + availableHeight;
        const ceilingY = paddingTop;
        const leftWallX = paddingLeft;
        const rightWallX = paddingLeft + availableWidth;
        const centerY = paddingTop + availableHeight / 2;
        
        // Paredes exactamente en los bordes del área con padding
        const floor = Bodies.rectangle(paddingLeft + availableWidth / 2, floorY + wallThickness / 2, availableWidth + wallThickness, wallThickness, boundaryOptions);
        const leftWall = Bodies.rectangle(leftWallX - wallThickness / 2, centerY, wallThickness, availableHeight + wallThickness, boundaryOptions);
        const rightWall = Bodies.rectangle(rightWallX + wallThickness / 2, centerY, wallThickness, availableHeight + wallThickness, boundaryOptions);
        const ceiling = Bodies.rectangle(paddingLeft + availableWidth / 2, ceilingY - wallThickness / 2, availableWidth + wallThickness, wallThickness, boundaryOptions);

        if (!textRef.current) return;
        
        const wordSpans = textRef.current.querySelectorAll<HTMLSpanElement>('.word');
        if (wordSpans.length === 0) {
          console.warn('FallingText: No word spans found');
          return;
        }
        
        // Separar las palabras y posicionarlas en sus posiciones actuales
        const wordBodies = Array.from(wordSpans).map((elem) => {
          const rect = elem.getBoundingClientRect();
          const containerRect = containerRef.current!.getBoundingClientRect();
          
          // Obtener la posición actual del elemento en el contenedor, respetando padding
          const currentLeft = rect.left - containerRect.left + rect.width / 2;
          const currentTop = rect.top - containerRect.top + rect.height / 2;
          
          // Asegurar que la posición esté dentro del área visible con padding
          // Usar un margen mínimo para que las palabras no queden pegadas a los bordes
          const minMargin = 20;
          const constrainedLeft = Math.max(
            paddingLeft + minMargin, 
            Math.min(paddingLeft + availableWidth - minMargin, currentLeft)
          );
          const constrainedTop = Math.max(
            paddingTop + minMargin, 
            Math.min(paddingTop + availableHeight - minMargin, currentTop)
          );
          
          // Usar las dimensiones reales del elemento
          const wordWidth = Math.max(rect.width || 50, 20);
          const wordHeight = Math.max(rect.height || 30, 20);

          const body = Bodies.rectangle(constrainedLeft, constrainedTop, wordWidth, wordHeight, {
            render: { fillStyle: 'transparent' },
            restitution: 0.8,
            frictionAir: 0.01,
            friction: 0.2
          });

          // Velocidad inicial aleatoria para que se "destruya"
          Matter.Body.setVelocity(body, {
            x: (Math.random() - 0.5) * 5,
            y: (Math.random() - 0.5) * 2
          });
          Matter.Body.setAngularVelocity(body, (Math.random() - 0.5) * 0.05);
          
          // Posicionar como absoluto
          elem.style.position = 'absolute';
          elem.style.left = `${constrainedLeft}px`;
          elem.style.top = `${constrainedTop}px`;
          elem.style.transform = 'translate(-50%, -50%)';
          
          return { elem, body };
        });

        const mouse = Mouse.create(containerRef.current);
        const mouseConstraint = MouseConstraint.create(engine, {
          mouse,
          constraint: {
            stiffness: mouseConstraintStiffness,
            render: { visible: false }
          }
        });
        render.mouse = mouse;

        World.add(engine.world, [floor, leftWall, rightWall, ceiling, mouseConstraint, ...wordBodies.map(wb => wb.body)]);

        const runner = Runner.create();
        Runner.run(runner, engine);
        Render.run(render);

        const updateLoop = () => {
          wordBodies.forEach(({ body, elem }) => {
            const { x, y } = body.position;
            // Las paredes de la física ya mantienen las palabras dentro, solo actualizar posición
            elem.style.left = `${x}px`;
            elem.style.top = `${y}px`;
            elem.style.transform = `translate(-50%, -50%) rotate(${body.angle}rad)`;
          });
          Matter.Engine.update(engine);
          requestAnimationFrame(updateLoop);
        };
        updateLoop();

        cleanup = () => {
          Render.stop(render);
          Runner.stop(runner);
          if (render.canvas && canvasContainerRef.current) {
            canvasContainerRef.current.removeChild(render.canvas);
          }
          World.clear(engine.world, false);
          Engine.clear(engine);
        };
      });
    });
    
    return () => {
      if (cleanup) cleanup();
    };
  }, [effectStarted, gravity, wireframes, backgroundColor, mouseConstraintStiffness, text]);

  const handleTrigger = () => {
    if (!effectStarted && (trigger === 'click' || trigger === 'hover')) {
      setEffectStarted(true);
    }
  };

  return (
    <div
      ref={containerRef}
      className="falling-text-container"
      onClick={trigger === 'click' ? handleTrigger : undefined}
      onMouseEnter={trigger === 'hover' ? handleTrigger : undefined}
      style={{
        position: 'relative',
        overflow: 'hidden',
        minWidth: '400px',
        minHeight: '200px',
        width: '100%',
        height: 'auto'
      }}
    >
      <div
        ref={textRef}
        className="falling-text-target"
        style={{
          fontSize: fontSize,
          lineHeight: 1.4,
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          pointerEvents: 'none'
        }}
      />
      <div ref={canvasContainerRef} className="falling-text-canvas" />
    </div>
  );
};

export default FallingText;
