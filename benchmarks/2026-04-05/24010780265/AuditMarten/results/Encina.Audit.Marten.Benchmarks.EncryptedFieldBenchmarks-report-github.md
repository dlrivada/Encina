```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                       | Mean       | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------- |-----------:|------:|------:|-----:|----------:|------------:|
| Encrypt_Short_16B            |   349.0 μs |    NA |  1.00 |    1 |    1720 B |        1.00 |
| Encrypt_Medium_256B          |   380.0 μs |    NA |  1.09 |    3 |    7792 B |        4.53 |
| Encrypt_Long_4KB             |   372.8 μs |    NA |  1.07 |    2 |   37336 B |       21.71 |
| Encrypt_VeryLong_64KB        |   572.9 μs |    NA |  1.64 |    5 |  569816 B |      331.29 |
| Decrypt_Short_16B            | 7,551.7 μs |    NA | 21.64 |    6 |     568 B |        0.33 |
| Decrypt_Medium_256B          | 7,906.8 μs |    NA | 22.66 |    9 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,612.8 μs |    NA | 21.82 |    7 |   16896 B |        9.82 |
| Decrypt_VeryLong_64KB        | 7,704.6 μs |    NA | 22.08 |    8 |  262656 B |      152.71 |
| DecryptOrPlaceholder_NullKey |   409.6 μs |    NA |  1.17 |    4 |         - |        0.00 |
