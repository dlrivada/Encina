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
| Encrypt_Short_16B            |   334.7 μs |    NA |  1.00 |    1 |    1720 B |        1.00 |
| Encrypt_Medium_256B          |   345.6 μs |    NA |  1.03 |    2 |    7792 B |        4.53 |
| Encrypt_Long_4KB             |   382.4 μs |    NA |  1.14 |    3 |   37336 B |       21.71 |
| Encrypt_VeryLong_64KB        |   583.4 μs |    NA |  1.74 |    5 |  569816 B |      331.29 |
| Decrypt_Short_16B            | 7,512.3 μs |    NA | 22.45 |    7 |     568 B |        0.33 |
| Decrypt_Medium_256B          | 7,510.8 μs |    NA | 22.44 |    6 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,629.4 μs |    NA | 22.80 |    8 |   16896 B |        9.82 |
| Decrypt_VeryLong_64KB        | 7,730.8 μs |    NA | 23.10 |    9 |  262656 B |      152.71 |
| DecryptOrPlaceholder_NullKey |   417.2 μs |    NA |  1.25 |    4 |         - |        0.00 |
