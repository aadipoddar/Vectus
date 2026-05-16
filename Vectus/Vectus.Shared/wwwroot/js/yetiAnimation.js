// Yeti Login Form Animation
// Adapted for Blazor integration

window.YetiAnimation = {
	mySVG: null,
	twoFingers: null,
	armL: null,
	armR: null,
	eyeL: null,
	eyeR: null,
	nose: null,
	mouth: null,
	mouthBG: null,
	mouthSmallBG: null,
	mouthMediumBG: null,
	mouthLargeBG: null,
	mouthMaskPath: null,
	mouthOutline: null,
	tooth: null,
	tongue: null,
	chin: null,
	face: null,
	eyebrow: null,
	outerEarL: null,
	outerEarR: null,
	earHairL: null,
	earHairR: null,
	hair: null,
	bodyBG: null,
	bodyBGchanged: null,

	emailInput: null,
	passwordInput: null,
	svgCoords: null,
	emailCoords: null,
	screenCenter: null,
	emailScrollMax: null,
	chinMin: 0.5,
	mouthStatus: "small",
	blinking: null,
	eyeScale: 1,
	eyesCovered: false,
	initialized: false,
	initRetryCount: 0,

	eyeLCoords: null,
	eyeRCoords: null,
	noseCoords: null,
	mouthCoords: null,

	init: function (emailInputId, passwordInputId) {
		// Prevent re-initialization
		if (this.initialized) {
			console.log('Yeti Animation: Already initialized');
			return;
		}

		// Get SVG elements
		this.mySVG = document.querySelector('.yeti-svg-container');
		this.twoFingers = document.querySelector('.twoFingers');
		this.armL = document.querySelector('.armL');
		this.armR = document.querySelector('.armR');
		this.eyeL = document.querySelector('.eyeL');
		this.eyeR = document.querySelector('.eyeR');
		this.nose = document.querySelector('.nose');
		this.mouth = document.querySelector('.mouth');
		this.mouthBG = document.querySelector('.mouthBG');
		this.mouthSmallBG = document.querySelector('.mouthSmallBG');
		this.mouthMediumBG = document.querySelector('.mouthMediumBG');
		this.mouthLargeBG = document.querySelector('.mouthLargeBG');
		this.mouthMaskPath = document.querySelector('#mouthMaskPath');
		this.mouthOutline = document.querySelector('.mouthOutline');
		this.tooth = document.querySelector('.tooth');
		this.tongue = document.querySelector('.tongue');
		this.chin = document.querySelector('.chin');
		this.face = document.querySelector('.face');
		this.eyebrow = document.querySelector('.eyebrow');
		this.outerEarL = document.querySelector('.earL .outerEar');
		this.outerEarR = document.querySelector('.earR .outerEar');
		this.earHairL = document.querySelector('.earL .earHair');
		this.earHairR = document.querySelector('.earR .earHair');
		this.hair = document.querySelector('.hair');
		this.bodyBG = document.querySelector('.bodyBGnormal');
		this.bodyBGchanged = document.querySelector('.bodyBGchanged');

		// Get input elements - Syncfusion TextBox creates nested input elements
		const emailContainer = document.querySelector(`[data-yeti-email="${emailInputId}"]`);
		const passwordContainer = document.querySelector(`[data-yeti-password="${passwordInputId}"]`);

		if (emailContainer) {
			// Syncfusion creates input inside .e-input-group
			this.emailInput = emailContainer.querySelector('input.e-input') ||
				emailContainer.querySelector('input[type="text"]') ||
				emailContainer.querySelector('input[type="email"]') ||
				emailContainer.querySelector('input');
		}

		if (passwordContainer) {
			this.passwordInput = passwordContainer.querySelector('input.e-input') ||
				passwordContainer.querySelector('input[type="password"]') ||
				passwordContainer.querySelector('input[type="text"]') ||
				passwordContainer.querySelector('input');
		}

		console.log('Yeti Animation Init:', {
			mySVG: this.mySVG,
			emailInput: this.emailInput,
			passwordInput: this.passwordInput,
			emailContainer: emailContainer,
			passwordContainer: passwordContainer
		});

		if (!this.mySVG || !this.emailInput) {
			this.initRetryCount++;
			if (this.initRetryCount < 10) {
				console.warn('Yeti Animation: Required elements not found. Retrying in 500ms... (attempt ' + this.initRetryCount + ')');
				setTimeout(() => this.init(emailInputId, passwordInputId), 500);
			} else {
				console.error('Yeti Animation: Failed to initialize after 10 attempts');
			}
			return;
		}

		// Mark as initialized
		this.initialized = true;
		console.log('Yeti Animation: Successfully initialized');

		// Calculate positions
		this.svgCoords = this.getPosition(this.mySVG);
		this.emailCoords = this.getPosition(this.emailInput);
		this.screenCenter = this.svgCoords.x + (this.mySVG.offsetWidth / 2);
		this.eyeLCoords = { x: this.svgCoords.x + 84, y: this.svgCoords.y + 76 };
		this.eyeRCoords = { x: this.svgCoords.x + 113, y: this.svgCoords.y + 76 };
		this.noseCoords = { x: this.svgCoords.x + 97, y: this.svgCoords.y + 81 };
		this.mouthCoords = { x: this.svgCoords.x + 100, y: this.svgCoords.y + 100 };

		// Check if GSAP is available
		if (typeof gsap === 'undefined') {
			console.error('Yeti Animation: GSAP library not loaded');
			return;
		}

		// Set initial arm positions
		gsap.set(this.armL, { x: -93, y: 220, rotation: 105, transformOrigin: "top left" });
		gsap.set(this.armR, { x: -93, y: 220, rotation: -105, transformOrigin: "top right" });
		gsap.set(this.mouth, { transformOrigin: "center center" });

		// Get email scroll max
		this.emailScrollMax = this.emailInput.scrollWidth;

		// Setup event listeners
		this.setupEventListeners();

		// Start blinking
		this.startBlinking(5);
	},

	setupEventListeners: function () {
		const self = this;

		if (this.emailInput) {
			this.emailInput.addEventListener('focus', function () {
				self.onEmailFocus();
			});
			this.emailInput.addEventListener('blur', function () {
				self.onEmailBlur();
			});
			this.emailInput.addEventListener('input', function () {
				self.onEmailInput();
			});
			// Also listen for keyup for better responsiveness
			this.emailInput.addEventListener('keyup', function () {
				self.onEmailInput();
			});
		}

		if (this.passwordInput) {
			this.passwordInput.addEventListener('focus', function () {
				self.onPasswordFocus();
			});
			this.passwordInput.addEventListener('blur', function () {
				self.onPasswordBlur();
			});
		}
	},

	onEmailFocus: function () {
		this.onEmailInput();
	},

	onEmailBlur: function () {
		this.resetFace();
	},

	onEmailInput: function () {
		this.calculateFaceMove();
		var value = this.emailInput.value;

		if (value.length > 0) {
			if (this.mouthStatus === "small") {
				this.mouthStatus = "medium";
				gsap.to([this.mouthBG, this.mouthOutline, this.mouthMaskPath], 1, { morphSVG: this.mouthMediumBG, shapeIndex: 8, ease: "expo.out" });
				gsap.to(this.tooth, 1, { x: 0, y: -2, ease: "expo.out" });
				gsap.to(this.tongue, 1, { x: 0, y: 1, ease: "expo.out" });
				gsap.to([this.eyeL, this.eyeR], 1, { scaleX: 0.85, scaleY: 0.85, ease: "expo.out" });
				this.eyeScale = 0.85;
			}
			if (value.includes("@")) {
				this.mouthStatus = "large";
				gsap.to([this.mouthBG, this.mouthOutline, this.mouthMaskPath], 1, { morphSVG: this.mouthLargeBG, shapeIndex: 9, ease: "expo.out" });
				gsap.to(this.tooth, 1, { x: 0, y: -2, ease: "expo.out" });
				gsap.to(this.tongue, 1, { y: 2, ease: "expo.out" });
				gsap.to([this.eyeL, this.eyeR], 1, { scaleX: 0.65, scaleY: 0.65, ease: "expo.out", transformOrigin: "center center" });
				this.eyeScale = 0.65;
			} else {
				this.mouthStatus = "medium";
				gsap.to([this.mouthBG, this.mouthOutline, this.mouthMaskPath], 1, { morphSVG: this.mouthMediumBG, shapeIndex: 8, ease: "expo.out" });
				gsap.to(this.tooth, 1, { x: 0, y: -2, ease: "expo.out" });
				gsap.to(this.tongue, 1, { x: 0, y: 1, ease: "expo.out" });
				gsap.to([this.eyeL, this.eyeR], 1, { scaleX: 0.85, scaleY: 0.85, ease: "expo.out" });
				this.eyeScale = 0.85;
			}
		} else {
			this.mouthStatus = "small";
			gsap.to([this.mouthBG, this.mouthOutline, this.mouthMaskPath], 1, { morphSVG: this.mouthSmallBG, shapeIndex: 9, ease: "expo.out" });
			gsap.to(this.tooth, 1, { x: 0, y: 0, ease: "expo.out" });
			gsap.to(this.tongue, 1, { y: 0, ease: "expo.out" });
			gsap.to([this.eyeL, this.eyeR], 1, { scaleX: 1, scaleY: 1, ease: "expo.out" });
			this.eyeScale = 1;
		}
	},

	calculateFaceMove: function () {
		if (!this.emailInput) return;

		var carPos = this.emailInput.selectionEnd;
		var div = document.createElement('div');
		var span = document.createElement('span');
		var copyStyle = getComputedStyle(this.emailInput);

		if (carPos == null || carPos == 0) {
			carPos = this.emailInput.value.length;
		}

		[].forEach.call(copyStyle, function (prop) {
			div.style[prop] = copyStyle[prop];
		});
		div.style.position = 'absolute';
		div.style.visibility = 'hidden';
		document.body.appendChild(div);
		div.textContent = this.emailInput.value.substr(0, carPos);
		span.textContent = this.emailInput.value.substr(carPos) || '.';
		div.appendChild(span);

		var caretCoords = {};
		var dFromC, eyeLAngle, eyeRAngle, noseAngle, mouthAngle;

		if (this.emailInput.scrollWidth <= this.emailScrollMax) {
			caretCoords = this.getPosition(span);
			dFromC = this.screenCenter - (caretCoords.x + this.emailCoords.x);
			eyeLAngle = this.getAngle(this.eyeLCoords.x, this.eyeLCoords.y, this.emailCoords.x + caretCoords.x, this.emailCoords.y + 25);
			eyeRAngle = this.getAngle(this.eyeRCoords.x, this.eyeRCoords.y, this.emailCoords.x + caretCoords.x, this.emailCoords.y + 25);
			noseAngle = this.getAngle(this.noseCoords.x, this.noseCoords.y, this.emailCoords.x + caretCoords.x, this.emailCoords.y + 25);
			mouthAngle = this.getAngle(this.mouthCoords.x, this.mouthCoords.y, this.emailCoords.x + caretCoords.x, this.emailCoords.y + 25);
		} else {
			eyeLAngle = this.getAngle(this.eyeLCoords.x, this.eyeLCoords.y, this.emailCoords.x + this.emailScrollMax, this.emailCoords.y + 25);
			eyeRAngle = this.getAngle(this.eyeRCoords.x, this.eyeRCoords.y, this.emailCoords.x + this.emailScrollMax, this.emailCoords.y + 25);
			noseAngle = this.getAngle(this.noseCoords.x, this.noseCoords.y, this.emailCoords.x + this.emailScrollMax, this.emailCoords.y + 25);
			mouthAngle = this.getAngle(this.mouthCoords.x, this.mouthCoords.y, this.emailCoords.x + this.emailScrollMax, this.emailCoords.y + 25);
			dFromC = this.screenCenter - (this.emailCoords.x + this.emailScrollMax);
		}

		var eyeLX = Math.cos(eyeLAngle) * 20;
		var eyeLY = Math.sin(eyeLAngle) * 10;
		var eyeRX = Math.cos(eyeRAngle) * 20;
		var eyeRY = Math.sin(eyeRAngle) * 10;
		var noseX = Math.cos(noseAngle) * 23;
		var noseY = Math.sin(noseAngle) * 10;
		var mouthX = Math.cos(mouthAngle) * 23;
		var mouthY = Math.sin(mouthAngle) * 10;
		var mouthR = Math.cos(mouthAngle) * 6;
		var chinX = mouthX * 0.8;
		var chinY = mouthY * 0.5;
		var chinS = 1 - ((dFromC * 0.15) / 100);
		if (chinS > 1) {
			chinS = 1 - (chinS - 1);
			if (chinS < this.chinMin) {
				chinS = this.chinMin;
			}
		}
		var faceX = mouthX * 0.3;
		var faceY = mouthY * 0.4;
		var faceSkew = Math.cos(mouthAngle) * 5;
		var eyebrowSkew = Math.cos(mouthAngle) * 25;
		var outerEarX = Math.cos(mouthAngle) * 4;
		var outerEarY = Math.cos(mouthAngle) * 5;
		var hairX = Math.cos(mouthAngle) * 6;
		var hairS = 1.2;

		gsap.to(this.eyeL, 1, { x: -eyeLX, y: -eyeLY, ease: "expo.out" });
		gsap.to(this.eyeR, 1, { x: -eyeRX, y: -eyeRY, ease: "expo.out" });
		gsap.to(this.nose, 1, { x: -noseX, y: -noseY, rotation: mouthR, transformOrigin: "center center", ease: "expo.out" });
		gsap.to(this.mouth, 1, { x: -mouthX, y: -mouthY, rotation: mouthR, transformOrigin: "center center", ease: "expo.out" });
		gsap.to(this.chin, 1, { x: -chinX, y: -chinY, scaleY: chinS, ease: "expo.out" });
		gsap.to(this.face, 1, { x: -faceX, y: -faceY, skewX: -faceSkew, transformOrigin: "center top", ease: "expo.out" });
		gsap.to(this.eyebrow, 1, { x: -faceX, y: -faceY, skewX: -eyebrowSkew, transformOrigin: "center top", ease: "expo.out" });
		gsap.to(this.outerEarL, 1, { x: outerEarX, y: -outerEarY, ease: "expo.out" });
		gsap.to(this.outerEarR, 1, { x: outerEarX, y: outerEarY, ease: "expo.out" });
		gsap.to(this.earHairL, 1, { x: -outerEarX, y: -outerEarY, ease: "expo.out" });
		gsap.to(this.earHairR, 1, { x: -outerEarX, y: outerEarY, ease: "expo.out" });
		gsap.to(this.hair, 1, { x: hairX, scaleY: hairS, transformOrigin: "center bottom", ease: "expo.out" });

		document.body.removeChild(div);
	},

	onPasswordFocus: function () {
		if (!this.eyesCovered) {
			this.coverEyes();
		}
	},

	onPasswordBlur: function () {
		this.uncoverEyes();
	},

	coverEyes: function () {
		gsap.killTweensOf([this.armL, this.armR]);
		gsap.set([this.armL, this.armR], { visibility: "visible" });
		gsap.to(this.armL, 0.45, { x: -93, y: 10, rotation: 0, ease: "quad.out" });
		gsap.to(this.armR, 0.45, { x: -93, y: 10, rotation: 0, ease: "quad.out", delay: 0.1 });
		gsap.to(this.bodyBG, 0.45, { morphSVG: this.bodyBGchanged, ease: "quad.out" });
		this.eyesCovered = true;
	},

	uncoverEyes: function () {
		gsap.killTweensOf([this.armL, this.armR]);
		gsap.to(this.armL, 1.35, { y: 220, ease: "quad.out" });
		gsap.to(this.armL, 1.35, { rotation: 105, ease: "quad.out", delay: 0.1 });
		gsap.to(this.armR, 1.35, { y: 220, ease: "quad.out" });
		var self = this;
		gsap.to(this.armR, 1.35, {
			rotation: -105, ease: "quad.out", delay: 0.1, onComplete: function () {
				gsap.set([self.armL, self.armR], { visibility: "hidden" });
			}
		});
		gsap.to(this.bodyBG, 0.45, { morphSVG: this.bodyBG, ease: "quad.out" });
		this.eyesCovered = false;
	},

	resetFace: function () {
		gsap.to([this.eyeL, this.eyeR], 1, { x: 0, y: 0, ease: "expo.out" });
		gsap.to(this.nose, 1, { x: 0, y: 0, scaleX: 1, scaleY: 1, ease: "expo.out" });
		gsap.to(this.mouth, 1, { x: 0, y: 0, rotation: 0, ease: "expo.out" });
		gsap.to(this.chin, 1, { x: 0, y: 0, scaleY: 1, ease: "expo.out" });
		gsap.to([this.face, this.eyebrow], 1, { x: 0, y: 0, skewX: 0, ease: "expo.out" });
		gsap.to([this.outerEarL, this.outerEarR, this.earHairL, this.earHairR, this.hair], 1, { x: 0, y: 0, scaleY: 1, ease: "expo.out" });
	},

	startBlinking: function (delay) {
		var self = this;
		if (delay) {
			delay = this.getRandomInt(delay);
		} else {
			delay = 1;
		}
		this.blinking = gsap.to([this.eyeL, this.eyeR], 0.1, {
			delay: delay, scaleY: 0, yoyo: true, repeat: 1, transformOrigin: "center center", onComplete: function () {
				self.startBlinking(12);
			}
		});
	},

	stopBlinking: function () {
		if (this.blinking) {
			this.blinking.kill();
			this.blinking = null;
			gsap.set([this.eyeL, this.eyeR], { scaleY: this.eyeScale });
		}
	},

	getRandomInt: function (max) {
		return Math.floor(Math.random() * Math.floor(max));
	},

	getAngle: function (x1, y1, x2, y2) {
		return Math.atan2(y1 - y2, x1 - x2);
	},

	getPosition: function (el) {
		var xPos = 0;
		var yPos = 0;

		while (el) {
			if (el.tagName === "BODY") {
				var xScroll = el.scrollLeft || document.documentElement.scrollLeft;
				var yScroll = el.scrollTop || document.documentElement.scrollTop;
				xPos += (el.offsetLeft - xScroll + el.clientLeft);
				yPos += (el.offsetTop - yScroll + el.clientTop);
			} else {
				xPos += (el.offsetLeft - el.scrollLeft + el.clientLeft);
				yPos += (el.offsetTop - el.scrollTop + el.clientTop);
			}
			el = el.offsetParent;
		}
		return { x: xPos, y: yPos };
	}
};
