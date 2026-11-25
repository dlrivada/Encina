```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host] : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

IterationCount=10  WarmupCount=3  

```
| Method               | Mean | Error |
|--------------------- |-----:|------:|
| EncryptString_Medium |   NA |    NA |

Benchmarks with issues:
  AesGcmFieldEncryptorBenchmarks.EncryptString_Medium: Job-YFEFPZ(IterationCount=10, WarmupCount=3)
