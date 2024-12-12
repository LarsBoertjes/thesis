import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

// to start up:
// npx serve . - to start a local server in the project's directory
// npx vite

// Renderer instance: set size at which we want to render our app
// Good idea: use width and height of area we want to fill our app - test case full window
const renderer = new THREE.WebGLRenderer();
renderer.setSize( window.innerWidth, window.innerHeight);
document.body.appendChild( renderer.domElement);

// Camera instance: perspective for now (different possible)
// fov: extent of the scene that is seen on the display (degrees)
// aspect ratio: almost always use width of element divided by height
// near and far clipping plane: further than far or closer than close won't get rendered
const camera = new THREE.PerspectiveCamera( 45, window.innerWidth / window.innerHeight, 1, 500);
camera.position.set(0, 0, 100);
camera.lookAt(0, 0, 0);

// Setting up a new scene
const scene = new THREE.Scene();

// Set up a material, for lines: LineBasicMaterial or LineDashedMaterial
const material = new THREE.LineBasicMaterial( { color : 0x0000ff });

const points = [];
points.push( new THREE.Vector3( -10, 0, 0 ));
points.push( new THREE.Vector3( 0, 10, 0));
points.push( new THREE.Vector3( 10, 0, 0));

const geometry = new THREE.BufferGeometry().setFromPoints(points);

const line = new THREE.Line( geometry, material);

const loader = new GLTFLoader();

loader.load

scene.add(line)
renderer.render(scene, camera);
