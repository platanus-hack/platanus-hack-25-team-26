import { Download, Shield, Eye, Users, AlertTriangle, CheckCircle, Menu, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { useState, useEffect } from 'react';
import koraImage from './kora.png';
import TextType from './TextType.tsx';
import BlurText from "./BlurText.tsx";
import GradientText from './GradientText.tsx';
import FallingText from './FallingText.tsx';
import RotatingText from './RotatingText'

function App() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [scrollY, setScrollY] = useState(0);
  const [currentFeatureIndex, setCurrentFeatureIndex] = useState(0);
  const [slidesToShow, setSlidesToShow] = useState(3);

  useEffect(() => {
    const handleScroll = () => setScrollY(window.scrollY);
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    const updateSlidesToShow = () => {
      if (window.innerWidth < 768) {
        setSlidesToShow(1);
      } else if (window.innerWidth < 1024) {
        setSlidesToShow(2);
      } else {
        setSlidesToShow(3);
      }
    };

    updateSlidesToShow();
    window.addEventListener('resize', updateSlidesToShow);
    return () => window.removeEventListener('resize', updateSlidesToShow);
  }, []);

  const features = [
              {
                icon: <Eye className="w-8 h-8 text-white" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Monitoreo Continuo',
                desc: 'K0ra analiza tu pantalla en tiempo real, vigilando cada sitio web y aplicación que uses para detectar amenazas al instante.',
                delay: '0s'
              },
              {
                icon: <AlertTriangle className="w-8 h-8 text-white" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Detección de Phishing',
                desc: 'Identificación automática de sitios web fraudulentos y correos maliciosos antes de que comprometan tu información.',
                delay: '0.1s'
              },
              {
                icon: <Users className="w-8 h-8 text-white" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Control Parental',
                desc: 'Protección avanzada contra grooming. K0ra detecta conversaciones sospechosas y protege a los menores en línea.',
                delay: '0.2s'
              },
              {
                icon: <Shield className="w-8 h-8 text-white" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Escudo Inteligente',
                desc: 'Tecnología de IA que aprende y se adapta a nuevas amenazas, manteniéndote siempre un paso adelante.',
                delay: '0.3s'
              },
              {
                icon: <CheckCircle className="w-8 h-8 text-white" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Alertas Instantáneas',
                desc: 'K0ra te notifica inmediatamente cuando detecta una amenaza, con información clara sobre qué hacer.',
                delay: '0.4s'
              },
              {
                icon: <img src={koraImage} alt="Kora" className="w-8 h-8 object-contain mix-blend-multiply" />,
                gradient: 'from-yellow-400 to-amber-500',
                title: 'Siempre Vigilante',
                desc: 'No es una extensión limitada. K0ra tiene acceso completo a tu pantalla para protegerte en todo momento.',
                delay: '0.5s'
              }
  ];

  const nextFeature = () => {
    setCurrentFeatureIndex((prev) => {
      const maxIndex = features.length - slidesToShow;
      return prev >= maxIndex ? 0 : prev + 1;
    });
  };

  const prevFeature = () => {
    setCurrentFeatureIndex((prev) => {
      const maxIndex = features.length - slidesToShow;
      return prev <= 0 ? maxIndex : prev - 1;
    });
  };

  const goToFeature = (index: number) => {
    const maxIndex = features.length - slidesToShow;
    setCurrentFeatureIndex(Math.min(index, maxIndex));
  };
  const handleAnimationComplete = () => {
    console.log('Animation completed!');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-slate-900 to-black">
      <nav className={`fixed top-0 w-full z-50 transition-premium ${
        scrollY > 50
          ? 'bg-gray-900/95 backdrop-blur-md border-b border-yellow-500/20 shadow-lg shadow-yellow-500/10'
          : 'bg-gray-900/40 backdrop-blur-sm'
      }`}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-2 animate-fade-in-up">
              <div className="w-10 h-10 bg-gradient-to-br from-yellow-400 to-amber-500 rounded-full flex items-center justify-center animate-pulse-glow">
                <span className="text-2xl">
                <img src={koraImage} alt="Kora" className="w-8 h-8 object-contain mix-blend-multiply" />
                </span>
              </div>
              <span className="text-3xl font-extrabold text-white tracking-tight">K0ra</span>
            </div>
            <div className="hidden md:flex space-x-8">
              <a href="#features" className="text-base text-gray-300 hover:text-yellow-400 transition-premium font-semibold">Características</a>
              <a href="#protection" className="text-base text-gray-300 hover:text-yellow-400 transition-premium font-semibold">Protección</a>
              <a href="#download" className="text-base text-gray-300 hover:text-yellow-400 transition-premium font-semibold">Descargar</a>
            </div>
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="md:hidden p-2 hover:bg-gray-800 rounded-lg transition-premium text-white"
            >
              {isMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
          {isMenuOpen && (
            <div className="md:hidden pb-4 space-y-2 animate-fade-in-up">
              <a href="#features" className="block px-4 py-2 text-gray-300 hover:bg-gray-800 rounded-lg transition-premium">Características</a>
              <a href="#protection" className="block px-4 py-2 text-gray-300 hover:bg-gray-800 rounded-lg transition-premium">Protección</a>
              <a href="#download" className="block px-4 py-2 text-gray-300 hover:bg-gray-800 rounded-lg transition-premium">Descargar</a>
            </div>
          )}
        </div>
      </nav>

      <section className="pt-32 pb-20 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto">
          <div className="text-center mb-16">
            <div className="inline-block mb-6 animate-fade-in-up">
              <div className="w-32 h-32 bg-gradient-to-br from-yellow-400 via-amber-500 to-yellow-500 rounded-full flex items-center justify-center animate-float">
                <span className="text-7xl">
                <img src={koraImage} alt="Kora" className="w-8 h-8 object-contain mix-blend-multiply" />
                </span>
              </div>
            </div>
            <h1 className="text-7xl md:text-7xl font-black text-white mb-8 animate-fade-in-up tracking-tight" style={{ animationDelay: '0.1s' }}>
            <TextType 
            text={["Conoce a K0ra", "Navega Protegido", "Seguridad digital"]}
            typingSpeed={75}
            pauseDuration={1500}
            showCursor={true}
            cursorCharacter="|"
/>
            </h1>
            <p className="text-xl md:text-2xl text-gray-300 max-w-3xl mx-auto mb-10 leading-relaxed font-light animate-fade-in-up" style={{ animationDelay: '0.2s' }}>
              Tu escudo inteligente de defensa del consumidor digital. 
            </p>
           
            <div className="flex flex-col sm:flex-row gap-4 justify-center items-center animate-fade-in-up" style={{ animationDelay: '0.4s' }}>
              <a
                href="#download"
                className="group relative px-8 py-4 bg-gradient-to-r from-yellow-500 to-amber-600 text-white rounded-xl font-bold text-xl  hover-lift flex items-center space-x-2 overflow-hidden"
              >
                <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity"></div>
                <Download className="w-5 h-5 relative z-10 group-hover:-translate-y-1 transition-transform" />
                <span className="relative z-10">Descargar Ahora</span>
              </a>
              <a
                href="#features"
                className="px-8 py-4 bg-gray-800 text-white rounded-xl font-bold text-xl shadow-lg hover-lift border-2 border-yellow-500/50 hover:border-yellow-400 transition-colors"
              >
                Ver Características
              </a>
            </div>
          </div>
        </div>
      </section>

      <section id="features" className="py-20 px-4 sm:px-6 lg:px-8 bg-gray-900/50">
        <div className="max-w-7xl mx-auto">
        <div className="flex justify-center items-center">
                      <BlurText
              text="¿Cómo te protege K0ra?"
              delay={150}
              animateBy="words"
              direction="top"
              onAnimationComplete={handleAnimationComplete}
              className="text-5xl md:text-6xl font-extrabold text-center text-white mb-16 animate-fade-in-up tracking-tight"
            />
         </div>
          
          {/* Carrusel */}
          <div className="relative">
            {/* Botones de navegación */}
            <button
              onClick={prevFeature}
              className="absolute left-0 top-1/2 -translate-y-1/2 -translate-x-4 md:-translate-x-12 z-10 w-12 h-12 md:w-16 md:h-16 bg-gray-800 rounded-full shadow-xl shadow-yellow-500/20 flex items-center justify-center hover:bg-gray-700 transition-premium border-2 border-yellow-500/50 hover:border-yellow-400 hover:scale-110"
              aria-label="Anterior"
            >
              <ChevronLeft className="w-6 h-6 md:w-8 md:h-8 text-yellow-400" />
            </button>
            
            <button
              onClick={nextFeature}
              className="absolute right-0 top-1/2 -translate-y-1/2 translate-x-4 md:translate-x-12 z-10 w-12 h-12 md:w-16 md:h-16 bg-gray-800 rounded-full shadow-xl shadow-yellow-500/20 flex items-center justify-center hover:bg-gray-700 transition-premium border-2 border-yellow-500/50 hover:border-yellow-400 hover:scale-110"
              aria-label="Siguiente"
            >
              <ChevronRight className="w-6 h-6 md:w-8 md:h-8 text-yellow-400" />
            </button>

            {/* Contenedor del carrusel */}
            <div className="overflow-hidden px-8 md:px-16">
              <div 
                className="flex transition-transform duration-500 ease-out gap-8"
                style={{
                  transform: `translateX(-${currentFeatureIndex * (100 / slidesToShow)}%)`
                }}
              >
                {features.map((feature, idx) => (
              <div
                key={idx}
                    className="flex-shrink-0 card-glow bg-gray-800/80 backdrop-blur-sm rounded-2xl p-8 shadow-xl shadow-yellow-500/10 hover-lift border border-yellow-500/20 hover:border-yellow-400/50 animate-fade-in-up"
                    style={{
                      width: `calc(${100 / slidesToShow}% - ${(slidesToShow - 1) * 2}rem / ${slidesToShow})`
                    }}
              >
                <div className={`w-16 h-16 bg-gradient-to-br ${feature.gradient} rounded-xl flex items-center justify-center mb-6 transform group-hover:scale-110 transition-transform duration-300 shadow-lg shadow-yellow-500/30`}>
                  {feature.icon}
                </div>
                    <h3 className="text-2xl md:text-3xl font-bold text-white mb-4">{feature.title}</h3>
                    <p className="text-base md:text-lg text-gray-300 leading-relaxed font-normal">{feature.desc}</p>
                  </div>
                ))}
              </div>
            </div>

            {/* Indicadores de puntos */}
            <div className="flex justify-center items-center gap-2 mt-8">
              {features.map((_, idx) => (
                <button
                  key={idx}
                  onClick={() => goToFeature(idx)}
                  className={`transition-all duration-300 rounded-full ${
                    idx === currentFeatureIndex
                      ? 'w-3 h-3 bg-yellow-500 scale-125 shadow-lg shadow-yellow-500/50'
                      : 'w-2 h-2 bg-yellow-500/40 hover:bg-yellow-500/60'
                  }`}
                  aria-label={`Ir a característica ${idx + 1}`}
                />
              ))}
            </div>
          </div>
        </div>
      </section>

      <section id="demo" className="py-20 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto">
          <div className="flex flex-row items-center justify-center mb-10 gap-2">
            <span className="text-white text-4xl md:text-5xl font-bold">Te presentamos Nuestra</span>
          <RotatingText
            texts={['Demo', 'Solución', 'IA', 'Tecnología']}
            mainClassName="px-2 sm:px-2 md:px-3 bg-yellow-300 text-black overflow-hidden py-0.5 sm:py-1 md:py-2 justify-center rounded-lg inline-flex items-center text-4xl md:text-5xl font-bold"
            staggerFrom={"last"}
            initial={{ y: "100%" }}
            animate={{ y: 0 }}
            exit={{ y: "-120%" }}
            staggerDuration={0.025}
            splitLevelClassName="overflow-hidden pb-0.5 sm:pb-1 md:pb-1"
            transition={{ type: "spring", damping: 30, stiffness: 400 }}
            rotationInterval={2000}
          /></div>
        
          <div className="flex flex-row gap-4 items-stretch min-h-[500px]">
            <div className="flex w-1/3 bg-gray-800/50 backdrop-blur-sm rounded-lg shadow-lg shadow-yellow-500/10 border border-yellow-500/20  items-stretch min-h-[500px]">
              <FallingText
                text={`Tu escudo digital de protección inteligente mientras navegas.`}
                highlightWords={["Seguridad", "Digital", "Protección", "Escudo", "Inteligente"]}
                highlightClass="highlighted"
                trigger="hover"
                backgroundColor="transparent"
                wireframes={false}
                gravity={0.56}
                fontSize="2rem"
                mouseConstraintStiffness={0.9}
              />
            </div>
            <div className="flex flex-col items-center justify-center w-2/3">
              <iframe
                src="https://www.youtube.com/embed/n5ttXELweg4"
                title="Kora Demo"
                className="w-full max-w-md aspect-video rounded-lg"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          </div>
        </div>
      </section>

      <section id="protection" className="py-20 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto">
          <div className="card-glow bg-gradient-to-br from-gray-800 via-slate-800 to-gray-900 rounded-3xl p-12 shadow-2xl shadow-yellow-500/20 border border-yellow-500/30 text-white overflow-hidden animate-fade-in-up">
            <div className="max-w-3xl mx-auto text-center relative z-10">
              <GradientText
                colors={["#fbbf24", "#f59e0b", "#fbbf24", "#fcd34d", "#fde68a"]}
                animationSpeed={3}
                showBorder={false}
                className="text-5xl md:text-6xl p-4 pb-2 font-black animate-fade-in-up tracking-tight leading-relaxed"
              >
                Tu Guardiana Digital
              </GradientText>
                            
              <p className="text-2xl md:text-3xl mt-5 mb-10 leading-relaxed text-gray-300 animate-fade-in-up font-light" style={{ animationDelay: '0.2s' }}>
                K0ra no descansa. Mientras navegas, trabajas o tus hijos juegan en línea, ella está ahí, protegiéndolos de las amenazas digitales más sofisticadas.
              </p>
              <div className="grid md:grid-cols-2 gap-8 text-left mt-12">
                <div className="bg-gray-900/50 backdrop-blur-sm rounded-xl p-6 border border-yellow-500/30 hover:border-yellow-400/50 hover-lift animate-slide-in-left" style={{ animationDelay: '0.3s' }}>
                  <h3 className="text-3xl font-extrabold mb-4 text-yellow-400">Contra Phishing</h3>
                  <p className="text-lg text-gray-300 font-normal">Detecta sitios falsos, correos fraudulentos y páginas maliciosas antes de que roben tu información.</p>
                </div>
                <div className="bg-gray-900/50 backdrop-blur-sm rounded-xl p-6 border border-yellow-500/30 hover:border-yellow-400/50 hover-lift animate-slide-in-right" style={{ animationDelay: '0.4s' }}>
                  <h3 className="text-3xl font-extrabold mb-4 text-yellow-400">Contra Grooming</h3>
                  <p className="text-lg text-gray-300 font-normal">Identifica patrones de comportamiento sospechoso en conversaciones para proteger a los menores.</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="download" className="py-20 px-4 sm:px-6 lg:px-8 bg-gray-900/50">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-5xl md:text-6xl font-extrabold text-white mb-8 animate-fade-in-up tracking-tight">
            Descarga <span className="text-yellow-400">K0ra</span>
          </h2>
          <p className="text-2xl md:text-3xl text-gray-300 mb-12 animate-fade-in-up font-light" style={{ animationDelay: '0.1s' }}>
            Disponible para Windows y macOS. Instalación rápida y protección inmediata.
          </p>
          <div className="flex flex-col sm:flex-row gap-6 justify-center items-center animate-fade-in-up" style={{ animationDelay: '0.2s' }}>
            <a
              href="https://github.com/platanus-hack/platanus-hack-25-team-26/releases/download/v1.0.0/release-v1.0.0-windows.zip"
              target="_blank"
              rel="noopener noreferrer"
              className="group w-full sm:w-auto px-10 py-6 bg-gradient-to-r from-yellow-500 to-amber-600 text-white rounded-2xl font-extrabold text-2xl hover-lift flex items-center justify-center space-x-3 relative overflow-hidden"
              aria-label="Descargar para Windows"
            >
              <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
              <Download className="w-6 h-6 relative z-10 group-hover:-translate-y-1 transition-transform duration-300" />
              <span className="relative z-10">Descargar para Windows</span>
            </a>
            <a
              href="https://github.com/platanus-hack/platanus-hack-25-team-26/releases/download/v1.0.0/releasev1.0.0-mac.zip"
              target="_blank"
              rel="noopener noreferrer"
              className="group w-full sm:w-auto px-10 py-6 bg-gradient-to-r from-yellow-400 to-amber-500 text-white rounded-2xl font-extrabold text-2xl hover-lift flex items-center justify-center space-x-3 relative overflow-hidden"
              aria-label="Descargar para macOS"
            >
              <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
              <Download className="w-6 h-6 relative z-10 group-hover:-translate-y-1 transition-transform duration-300" />
              <span className="relative z-10">Descargar para macOS</span>
            </a>
          </div>
          <p className="mt-8 text-lg md:text-xl text-gray-400 animate-fade-in-up font-medium" style={{ animationDelay: '0.3s' }}>
            Versión gratuita disponible. Protección completa sin costo.
          </p>
        </div>
      </section>

      <footer className="bg-gradient-to-r from-gray-900 via-slate-900 to-black text-white py-12 px-4 sm:px-6 lg:px-8 border-t border-yellow-500/20">
        <div className="max-w-7xl mx-auto text-center">
          <div className="flex items-center justify-center space-x-3 mb-6 animate-fade-in-up">
            <div className="w-12 h-12 bg-gradient-to-br from-yellow-400 to-amber-500 rounded-full flex items-center justify-center animate-pulse-glow">
              <span className="text-3xl">
              <img src={koraImage} alt="Kora" className="w-8 h-8 object-contain mix-blend-multiply" />
              </span>
            </div>
            <span className="text-4xl font-black tracking-tight">K0ra</span>
          </div>
          <p className="text-2xl mb-4 text-gray-300 animate-fade-in-up font-semibold" style={{ animationDelay: '0.1s' }}>
            La seguridad digital no es un privilegio, es un derecho
          </p>
          
        </div>
      </footer>
    </div>
  );
}

export default App;
