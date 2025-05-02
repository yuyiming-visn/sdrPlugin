#ifndef CDR_DEMOD_H 
#define CDR_DEMOD_H 

#include <complex.h>

#define cdr_complex_Zero  (0.0f + 0.0fI)

#define cdr_NO_ERROR   0 

typedef unsigned char  cdr_byte;
typedef unsigned int   cdr_uint;
typedef float _Complex cdr_complex;

typedef struct cdr_byteArray
{
    cdr_byte* handle;
    int totalSize;
    int validLength;
}cdr_byteArray;

typedef struct cdr_complexArray
{
    cdr_complex* handle;
    int totalSize;
    int validLength;
}cdr_complexArray;

typedef struct cdr_complexArraySub
{
    cdr_complex* handle;
    int subLength;
}cdr_complexArraySub;

typedef struct cdr_complexFrame
{
    cdr_complexArray     *dataHandle;
    cdr_complexArraySub  **linePosition;
    int                  numOfLines;
}cdr_complexFrame;

typedef struct cdr_uintArray
{
    cdr_uint* handle;
    int totalSize;
    int validLength;
}cdr_uintArray;

/* LDPCCodeRate */
enum LDPCRate { Rate1_4 = 0, Rate1_3 = 1, Rate1_2 = 2, Rate3_4 = 3 };

/* 传输模式 */
enum TransmissionMode { TransMode1 = 1, TransMode2 = 2, TransMode3 = 3 };

/* 频谱模式 */
enum SpectrumType { SpecMode1 = 1, SpecMode2 = 2, SpecMode9 = 9, SpecMode10 = 10, SpecMode22 = 22, SpecMode23 = 23 };

/* 子带标称频率 */
enum SubBandFreq { SubF000k = 0, SubF050k = 1, SubF100k = 2, SubF150k = 3, SubF200k = 4, };

/* 分层调制 */
enum HierarchicalMod { HierarchyNone = 0, HierarchyAlpha1 = 1, HierarchyAlpha2 = 2, HierarchyAlpha4 = 3, };

/* QAM */
enum QamType { QPSK = 0, QAM16 = 1, QAM64 = 2, };

typedef void* cdrDemodHandle;

#ifdef __cplusplus  
extern "C" {
#endif  

cdrDemodHandle CDRDemodulation_Init(void);

void CDRDemodulation_Release(cdrDemodHandle handle);

int CDRDemodulation_Process(cdrDemodHandle handle, cdr_complex* iqData, int iqLength);

void CDRDemodulation_Reset(cdrDemodHandle handle);

cdr_byte* CDRDemodulation_GetDraStream (cdrDemodHandle handle, int programeID, int *length);

int CDRDemodulation_GetNumOfPrograms (cdrDemodHandle handle);

enum QamType CDRDemodulation_GetSDISModType (cdrDemodHandle handle);

enum QamType CDRDemodulation_GetMSDSModType (cdrDemodHandle handle);

enum LDPCRate CDRDemodulation_GetLDPCRate1 (cdrDemodHandle handle);

void CDRDemodulation_SetTransferMode(cdrDemodHandle handle, enum TransmissionMode transferMode);

void CDRDemodulation_SetSpectrumMode(cdrDemodHandle handle, enum SpectrumType spectrumMode);

double CDRDemodulation_GetFrequencyOffset(cdrDemodHandle handle);

double CDRDemodulation_GetSampleRateOffset(cdrDemodHandle handle);

#ifdef __cplusplus  
}
#endif  

#endif
