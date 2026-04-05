```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                       | Mean       | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------- |-----------:|------:|------:|-----:|----------:|------------:|
| Encrypt_Short_16B            |   334.9 μs |    NA |  1.00 |    1 |    1720 B |        1.00 |
| Encrypt_Medium_256B          |   335.9 μs |    NA |  1.00 |    2 |    7792 B |        4.53 |
| Encrypt_Long_4KB             |   353.4 μs |    NA |  1.06 |    3 |   37336 B |       21.71 |
| Encrypt_VeryLong_64KB        |   510.5 μs |    NA |  1.52 |    5 |  569816 B |      331.29 |
| Decrypt_Short_16B            | 7,130.6 μs |    NA | 21.29 |    8 |     568 B |        0.33 |
| Decrypt_Medium_256B          | 7,101.5 μs |    NA | 21.21 |    7 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,005.2 μs |    NA | 20.92 |    6 |   16896 B |        9.82 |
| Decrypt_VeryLong_64KB        | 7,380.4 μs |    NA | 22.04 |    9 |  262656 B |      152.71 |
| DecryptOrPlaceholder_NullKey |   397.1 μs |    NA |  1.19 |    4 |         - |        0.00 |
