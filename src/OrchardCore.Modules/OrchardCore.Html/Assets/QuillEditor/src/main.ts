import { createApp, ref } from 'vue';
import './style.css';

export const initQuillEditor = (selector: string) => {
    createApp({
        setup() {
        const message = ref('Hello Vue!')
            return {
                message
            }
        }
    }).mount(selector)
};
