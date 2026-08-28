using System;

using UnityEngine;

namespace ProjectR.Backrooms.Audio
{
    /// <summary>
    /// 실행 중에 임시 사운드를 합성해 주는 도구입니다.
    /// </summary>
    /// <remarks>
    /// 프로토타입 단계에서 소리 없이 테스트하면 "몬스터 없이 무서운가"라는 질문의 답이 나오지 않습니다.
    /// 그래서 진짜 음원이 준비되기 전까지 형광등 험과 발소리를 코드로 만들어 씁니다.
    /// 음원이 준비되면 <c>SWAudioLibrary</c>에 키로 등록하고 <c>SWAudioManager</c>로 재생하도록 바꿉니다.
    /// SWAudioLibrary는 직렬화된 클립만 다루므로 실행 중에 만든 클립을 등록할 수 없어,
    /// 지금은 재생만 <see cref="AudioSource"/>로 직접 합니다.
    /// 같은 시드에서 항상 같은 소리가 나오도록 난수는 고정 시드로 만듭니다.
    /// </remarks>
    public static class ProceduralAudioBank
    {
        #region 필드
        /// <summary>합성에 사용할 표본 수(Hz)입니다.</summary>
        private const int SampleRate = 44100;

        /// <summary>소리를 항상 같게 만들기 위한 고정 시드입니다.</summary>
        private const int NoiseSeed = 20260827;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 형광등이 웅웅거리는 소리를 만듭니다. 이어 붙여도 끊기지 않습니다.
        /// </summary>
        /// <returns>1초 길이의 반복 재생용 클립입니다.</returns>
        /// <remarks>
        /// 60Hz 전원에서 나는 소리이므로 120Hz를 기본으로 잡고 배음을 얹었습니다.
        /// 길이를 1초로 맞추면 모든 주파수가 정수 번 반복되어 이음매가 생기지 않습니다.
        /// </remarks>
        public static AudioClip CreateFluorescentHum()
        {
            int sampleCount = SampleRate;
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed);

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float flutter = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 4f * time);

                float value = 0f;
                value += 1.00f * Mathf.Sin(2f * Mathf.PI * 120f * time);
                value += 0.42f * Mathf.Sin(2f * Mathf.PI * 240f * time);
                value += 0.20f * Mathf.Sin(2f * Mathf.PI * 360f * time);
                value += 0.09f * Mathf.Sin(2f * Mathf.PI * 600f * time);
                value += 0.05f * NextNoise(random);

                samples[index] = value * flutter * 0.22f;
            }

            MakeSeamless(samples, SampleRate / 200);

            return CreateClip("Hum_Fluorescent", samples);
        }

        /// <summary>
        /// 아무 소리도 없을 때 깔아 둘 공간의 바닥 소음을 만듭니다.
        /// </summary>
        /// <returns>4초 길이의 반복 재생용 클립입니다.</returns>
        /// <remarks>완전한 무음은 오히려 부자연스러워서 아주 낮은 저역 소음을 깔아 둡니다.</remarks>
        public static AudioClip CreateRoomTone()
        {
            int sampleCount = SampleRate * 4;
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 1);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.02f);
                samples[index] = lowPassed * 0.5f;
            }

            MakeSeamless(samples, SampleRate / 4);

            return CreateClip("Tone_Room", samples);
        }

        /// <summary>
        /// 어딘가에서 물이 한 방울 떨어지는 소리를 만듭니다.
        /// </summary>
        /// <returns>물방울 소리 클립입니다.</returns>
        /// <remarks>떨어지면서 음이 낮아지는 짧은 사인파로 만듭니다.</remarks>
        public static AudioClip CreateWaterDrip()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.35f);
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float frequency = Mathf.Lerp(1350f, 620f, Mathf.Clamp01(time / 0.12f));

                phase += 2f * Mathf.PI * frequency / SampleRate;
                samples[index] = Mathf.Sin(phase) * Mathf.Exp(-13f * time) * 0.5f;
            }

            ApplyAttack(samples, SampleRate / 800);

            return CreateClip("Ambient_Drip", samples);
        }

        /// <summary>
        /// 먼 곳에서 무언가 부딪히는 둔한 소리를 만듭니다.
        /// </summary>
        /// <returns>먼 충격음 클립입니다.</returns>
        /// <remarks>거리감은 고역을 걷어 내 만듭니다.</remarks>
        public static AudioClip CreateDistantThud()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.9f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 2);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;

                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.012f);

                float body = Mathf.Sin(2f * Mathf.PI * 54f * time) * Mathf.Exp(-9f * time) * 0.6f;

                samples[index] = (lowPassed * 3.5f + body) * Mathf.Exp(-4.5f * time) * 0.55f;
            }

            ApplyAttack(samples, SampleRate / 300);

            return CreateClip("Ambient_Thud", samples);
        }

        /// <summary>
        /// 환풍기가 한 차례 돌아가는 바람 소리를 만듭니다.
        /// </summary>
        /// <returns>환풍기 소리 클립입니다.</returns>
        public static AudioClip CreateVentGust()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 2.8f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 3);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float progress = index / (float)sampleCount;

                // 커졌다 작아지는 모양으로 만들어 지나가는 바람처럼 들리게 합니다.
                float swell = Mathf.Sin(Mathf.PI * progress);

                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.09f);

                float wobble = 1f + 0.25f * Mathf.Sin(2f * Mathf.PI * 7f * time);

                samples[index] = lowPassed * swell * wobble * 0.6f;
            }

            return CreateClip("Ambient_Vent", samples);
        }

        /// <summary>
        /// 바닥 재질에 맞는 발소리 한 걸음을 만듭니다.
        /// </summary>
        /// <param name="surface">발소리를 만들 바닥 재질입니다.</param>
        /// <param name="variation">같은 재질에서 조금씩 다른 소리를 얻기 위한 번호입니다.</param>
        /// <returns>한 걸음 길이의 클립입니다.</returns>
        public static AudioClip CreateFootstep(EFootstepSurface surface, int variation)
        {
            GetFootstepShape(surface, out float durationSeconds, out float decayRate,
                out float brightness, out float thumpLevel, out float level);

            int sampleCount = Mathf.RoundToInt(SampleRate * durationSeconds);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + (int)surface * 31 + variation);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float envelope = Mathf.Exp(-decayRate * time);

                // 발이 닿는 순간의 마찰음은 잡음을, 무게감은 낮은 사인파를 씁니다.
                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), brightness);

                float thump = Mathf.Sin(2f * Mathf.PI * 78f * time) * Mathf.Exp(-38f * time) * thumpLevel;

                samples[index] = (lowPassed + thump) * envelope * level;
            }

            ApplyAttack(samples, SampleRate / 500);

            return CreateClip($"Footstep_{surface}_{variation}", samples);
        }

        /// <summary>
        /// 몬스터가 배회하거나 돌아갈 때 내는 낮은 그르렁을 만듭니다.
        /// </summary>
        /// <returns>그르렁 소리 클립입니다.</returns>
        /// <remarks>
        /// 낮은 소리는 멀리서도 들리지만 어디서 나는지는 잘 잡히지 않습니다.
        /// "근처에 있다"까지만 알리고 "어디 있다"는 알리지 않으려고 저역만 씁니다.
        /// </remarks>
        public static AudioClip CreateMonsterGrowl()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 1.6f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 11);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float progress = index / (float)sampleCount;
                float swell = Mathf.Sin(Mathf.PI * progress);

                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.02f);

                // 두 개의 낮은 주파수를 살짝 어긋나게 겹쳐 목이 떨리는 느낌을 만듭니다.
                float body = Mathf.Sin(2f * Mathf.PI * 62f * time) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * 71f * time) * 0.35f;

                samples[index] = (lowPassed * 4f + body) * swell * 0.5f;
            }

            ApplyAttack(samples, SampleRate / 60);

            return CreateClip("Monster_Growl", samples);
        }

        /// <summary>
        /// 몬스터가 추격으로 들어갈 때 내는 날카로운 소리를 만듭니다.
        /// </summary>
        /// <returns>추격 시작 소리 클립입니다.</returns>
        /// <remarks>
        /// 이 소리 하나가 "들켰다"를 알립니다. 들리지 않으면 왜 쫓기는지 모른 채 죽게 되므로
        /// 다른 소리보다 확실히 높고 크게 만듭니다.
        /// </remarks>
        public static AudioClip CreateMonsterScreech()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 1.1f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 12);

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float progress = index / (float)sampleCount;
                float envelope = Mathf.Exp(-2.6f * time);

                // 주파수를 위로 훑어 올려 비명처럼 들리게 합니다.
                float sweep = Mathf.Lerp(420f, 980f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * sweep * time)
                    + 0.45f * Mathf.Sin(2f * Mathf.PI * sweep * 2.02f * time);

                samples[index] = (tone + NextNoise(random) * 0.35f) * envelope * 0.45f;
            }

            ApplyAttack(samples, SampleRate / 400);

            return CreateClip("Monster_Screech", samples);
        }

        /// <summary>
        /// 몬스터가 무언가를 찾거나 기다릴 때 내는 숨소리를 만듭니다.
        /// </summary>
        /// <returns>숨소리 클립입니다.</returns>
        /// <remarks>들이쉬고 내쉬는 두 마디로 만들어 살아 있는 것처럼 들리게 합니다.</remarks>
        public static AudioClip CreateMonsterBreath()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 2.2f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 13);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float progress = index / (float)sampleCount;

                // 앞 절반은 들이쉬고 뒤 절반은 내쉬는 모양입니다.
                float envelope = progress < 0.45f
                    ? Mathf.Sin(Mathf.PI * (progress / 0.45f))
                    : Mathf.Sin(Mathf.PI * ((progress - 0.55f) / 0.45f)) * 0.75f;

                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.05f);

                samples[index] = lowPassed * Mathf.Max(0f, envelope) * 1.1f;
            }

            return CreateClip("Monster_Breath", samples);
        }

        /// <summary>
        /// 몬스터의 무거운 발소리 한 걸음을 만듭니다.
        /// </summary>
        /// <param name="variation">같은 소리가 반복되지 않게 하기 위한 번호입니다.</param>
        /// <returns>한 걸음 길이의 클립입니다.</returns>
        /// <remarks>
        /// 발소리가 멀어지다 멈추는 순간이 가장 강한 연출이므로 플레이어 발소리와 확실히 구분되어야 합니다.
        /// 플레이어보다 낮고 길게 끌리도록 만들었습니다.
        /// </remarks>
        public static AudioClip CreateMonsterFootstep(int variation)
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.42f);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(NoiseSeed + 14 + variation);
            float lowPassed = 0f;

            for (int index = 0; index < sampleCount; index += 1)
            {
                float time = index / (float)SampleRate;
                float envelope = Mathf.Exp(-13f * time);

                lowPassed = Mathf.Lerp(lowPassed, NextNoise(random), 0.09f);

                float thump = Mathf.Sin(2f * Mathf.PI * 46f * time) * Mathf.Exp(-19f * time);

                samples[index] = (lowPassed * 0.8f + thump * 1.3f) * envelope * 0.7f;
            }

            ApplyAttack(samples, SampleRate / 400);

            return CreateClip($"Monster_Footstep_{variation}", samples);
        }

        /// <summary>
        /// 재질별 발소리의 모양을 구합니다.
        /// </summary>
        /// <param name="surface">기준이 되는 바닥 재질입니다.</param>
        /// <param name="durationSeconds">소리의 길이(초)입니다.</param>
        /// <param name="decayRate">소리가 잦아드는 빠르기입니다. 클수록 짧게 끊깁니다.</param>
        /// <param name="brightness">잡음이 얼마나 밝은지입니다. 1에 가까울수록 고역이 살아 있습니다.</param>
        /// <param name="thumpLevel">발이 닿는 무게감의 크기입니다.</param>
        /// <param name="level">전체 크기입니다.</param>
        private static void GetFootstepShape(EFootstepSurface surface, out float durationSeconds,
            out float decayRate, out float brightness, out float thumpLevel, out float level)
        {
            switch (surface)
            {
                case EFootstepSurface.Tile:
                    durationSeconds = 0.26f;
                    decayRate = 22f;
                    brightness = 0.55f;
                    thumpLevel = 0.35f;
                    level = 0.55f;
                    return;

                case EFootstepSurface.Carpet:
                    durationSeconds = 0.16f;
                    decayRate = 46f;
                    brightness = 0.07f;
                    thumpLevel = 0.55f;
                    level = 0.32f;
                    return;

                default:
                    durationSeconds = 0.22f;
                    decayRate = 30f;
                    brightness = 0.28f;
                    thumpLevel = 0.5f;
                    level = 0.5f;
                    return;
            }
        }

        /// <summary>
        /// -1에서 1 사이의 잡음 표본을 하나 뽑습니다.
        /// </summary>
        /// <param name="random">사용할 난수기입니다.</param>
        /// <returns>잡음 표본입니다.</returns>
        private static float NextNoise(System.Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }

        /// <summary>
        /// 시작 부분에 짧은 페이드인을 넣어 딱 소리가 나지 않게 합니다.
        /// </summary>
        /// <param name="samples">다듬을 표본 배열입니다.</param>
        /// <param name="attackSamples">페이드인에 쓸 표본 수입니다.</param>
        private static void ApplyAttack(float[] samples, int attackSamples)
        {
            int count = Mathf.Min(attackSamples, samples.Length);

            for (int index = 0; index < count; index += 1)
            {
                samples[index] *= index / (float)count;
            }
        }

        /// <summary>
        /// 끝부분을 앞부분과 겹쳐 이어 붙여도 끊기지 않게 만듭니다.
        /// </summary>
        /// <param name="samples">다듬을 표본 배열입니다.</param>
        /// <param name="fadeSamples">겹칠 표본 수입니다.</param>
        /// <remarks>잡음은 주기가 없어서 그냥 이으면 이음매에서 딱 소리가 납니다.</remarks>
        private static void MakeSeamless(float[] samples, int fadeSamples)
        {
            int count = Mathf.Min(fadeSamples, samples.Length / 2);

            for (int index = 0; index < count; index += 1)
            {
                float weight = index / (float)count;
                int tailIndex = samples.Length - count + index;

                samples[tailIndex] = Mathf.Lerp(samples[tailIndex], samples[index], weight);
            }
        }

        /// <summary>
        /// 표본 배열로 실제 오디오 클립을 만듭니다.
        /// </summary>
        /// <param name="clipName">클립 이름입니다.</param>
        /// <param name="samples">클립에 담을 표본 배열입니다.</param>
        /// <returns>만들어진 오디오 클립입니다.</returns>
        private static AudioClip CreateClip(string clipName, float[] samples)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);

            clip.SetData(samples, 0);

            return clip;
        }
        #endregion // 함수
    }
}
