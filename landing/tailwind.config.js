/** @type {import('tailwindcss').Config} */
export default {
    darkMode: ['class'],
    content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
  	extend: {
  		fontFamily: {
  			sans: [
  				'Poppins',
  				'sans-serif'
  			],
  			display: [
  				'Poppins',
  				'sans-serif'
  			],
  			heading: [
  				'Poppins',
  				'sans-serif'
  			],
  			body: [
  				'Poppins',
  				'sans-serif'
  			],
  			accent: [
  				'Poppins',
  				'sans-serif'
  			]
  		},
  		colors: {
  			yellow: {
  				50: '#FFFEF9',
  				100: '#FFF9E6',
  				200: '#FFF3CC',
  				300: '#FFE9A8',
  				400: '#FFD700',
  				500: '#F4D03F',
  				600: '#D4AF37',
  				700: '#B8941F',
  				800: '#9A7A0F',
  				900: '#7D5F00',
  				950: '#5C4500'
  			},
  			amber: {
  				50: '#FFFBEB',
  				100: '#FEF3C7',
  				200: '#FDE68A',
  				300: '#FCD34D',
  				400: '#FBBF24',
  				500: '#F59E0B',
  				600: '#D97706',
  				700: '#B45309',
  				800: '#92400E',
  				900: '#78350F',
  				950: '#451A03'
  			},
  			gold: {
  				50: '#FFFEF9',
  				100: '#FFF9E6',
  				200: '#FFF3CC',
  				300: '#FFE9A8',
  				400: '#FFD700',
  				500: '#F4D03F',
  				600: '#D4AF37',
  				700: '#C9A227',
  				800: '#B8941F',
  				900: '#9A7A0F'
  			}
  		},
  		borderRadius: {
  			lg: 'var(--radius)',
  			md: 'calc(var(--radius) - 2px)',
  			sm: 'calc(var(--radius) - 4px)'
  		},
  		colors: {
  			background: 'hsl(var(--background))',
  			foreground: 'hsl(var(--foreground))',
  			card: {
  				DEFAULT: 'hsl(var(--card))',
  				foreground: 'hsl(var(--card-foreground))'
  			},
  			popover: {
  				DEFAULT: 'hsl(var(--popover))',
  				foreground: 'hsl(var(--popover-foreground))'
  			},
  			primary: {
  				DEFAULT: 'hsl(var(--primary))',
  				foreground: 'hsl(var(--primary-foreground))'
  			},
  			secondary: {
  				DEFAULT: 'hsl(var(--secondary))',
  				foreground: 'hsl(var(--secondary-foreground))'
  			},
  			muted: {
  				DEFAULT: 'hsl(var(--muted))',
  				foreground: 'hsl(var(--muted-foreground))'
  			},
  			accent: {
  				DEFAULT: 'hsl(var(--accent))',
  				foreground: 'hsl(var(--accent-foreground))'
  			},
  			destructive: {
  				DEFAULT: 'hsl(var(--destructive))',
  				foreground: 'hsl(var(--destructive-foreground))'
  			},
  			border: 'hsl(var(--border))',
  			input: 'hsl(var(--input))',
  			ring: 'hsl(var(--ring))',
  			chart: {
  				'1': 'hsl(var(--chart-1))',
  				'2': 'hsl(var(--chart-2))',
  				'3': 'hsl(var(--chart-3))',
  				'4': 'hsl(var(--chart-4))',
  				'5': 'hsl(var(--chart-5))'
  			}
  		}
  	}
  },
  plugins: [require("tailwindcss-animate")],
};
