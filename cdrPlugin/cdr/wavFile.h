
#ifndef WAV_FILE_H  
#define WAV_FILE_H 

typedef void* wavHandle;

#ifdef __cplusplus  
extern "C" {
#endif

void wav_SetChannels (wavHandle handle, int channels);

void wav_SetSampleRate (wavHandle handle, int samplerate);

void wav_SetSampleBits (wavHandle handle, int samplebits);

int wav_GetChannels (wavHandle handle);

int wav_GetSampleRate (wavHandle handle);

int wav_GetSampleBits (wavHandle handle);

wavHandle wav_OpenWaveFile (const char *filename);

wavHandle wav_CreateFile (const char *filename, int channels, int samplerate, int samplebits);

void wav_CloseFile (wavHandle handle);

int wav_WriteShort(wavHandle handle, short *data, int samples);

int wav_WriteByte(wavHandle handle, char *data, int samples);

int wav_ReadShort(wavHandle handle, short *buffer, int samples);

int wav_ReadByte(wavHandle handle, char *buffer, int samples);

int wav_Seek(wavHandle handle, int offset, int origin);

#ifdef __cplusplus  
}
#endif  

#endif
