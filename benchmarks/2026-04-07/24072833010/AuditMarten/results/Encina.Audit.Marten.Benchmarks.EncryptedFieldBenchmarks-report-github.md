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
| Encrypt_Medium_256B          |   352.2 μs |    NA |  1.15 |    2 |    7792 B |        4.53 |
| Encrypt_VeryLong_64KB        |   503.4 μs |    NA |  1.65 |    5 |  569816 B |      331.29 |
| Decrypt_Medium_256B          | 7,201.6 μs |    NA | 23.57 |    7 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,204.7 μs |    NA | 23.58 |    8 |   16896 B |        9.82 |
| DecryptOrPlaceholder_NullKey |   410.3 μs |    NA |  1.34 |    4 |         - |        0.00 |
| Decrypt_Short_16B            | 7,171.8 μs |    NA | 23.48 |    6 |     568 B |        0.33 |
| Decrypt_VeryLong_64KB        | 7,438.6 μs |    NA | 24.35 |    9 |  262656 B |      152.71 |
| Encrypt_Long_4KB             |   360.6 μs |    NA |  1.18 |    3 |   37336 B |       21.71 |
| Encrypt_Short_16B            |   305.5 μs |    NA |  1.00 |    1 |    1720 B |        1.00 |
