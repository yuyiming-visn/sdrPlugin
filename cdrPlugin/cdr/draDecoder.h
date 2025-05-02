
#ifndef DRA_DECODER_H  
#define DRA_DECODER_H 

typedef void* draDecoderHandle;

typedef unsigned char  dra_byte;

typedef short          dra_short;

typedef struct dra_byteArray
{
    dra_byte* handle;
    int totalSize;
    int validLength;
}dra_byteArray;

typedef struct dra_shortArray
{
    short* handle;
    int totalSize;
    int validLength;
}dra_shortArray;

#ifdef __cplusplus  
extern "C" {
#endif  

    draDecoderHandle draDecoder_Init(void);

    void draDecoder_Release(draDecoderHandle pDraHandle);

    int draDecoder_Proccess(draDecoderHandle pDraHandle, dra_byte *inputBuffer, int inputLength);

    int draDecoder_GetMaxInputDataSize (draDecoderHandle handle);

    int draDecoder_GetAudioChannels(draDecoderHandle pDraHandle);

    int draDecoder_GetAudioSampleRate(draDecoderHandle pDraHandle);

    dra_short* draDecoder_GetAudioStream (draDecoderHandle handle, int *streamLength);

#ifdef __cplusplus  
}
#endif  

#endif
