```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                       | Mean       | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------- |-----------:|------:|------:|-----:|----------:|------------:|
| Encrypt_Short_16B            |   357.4 μs |    NA |  1.00 |    2 |    1720 B |        1.00 |
| Encrypt_Medium_256B          |   355.3 μs |    NA |  0.99 |    1 |    7792 B |        4.53 |
| Encrypt_Long_4KB             |   409.0 μs |    NA |  1.14 |    4 |   37336 B |       21.71 |
| Encrypt_VeryLong_64KB        |   607.1 μs |    NA |  1.70 |    5 |  569816 B |      331.29 |
| Decrypt_Short_16B            | 8,148.8 μs |    NA | 22.80 |    9 |     568 B |        0.33 |
| Decrypt_Medium_256B          | 7,680.9 μs |    NA | 21.49 |    6 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,791.0 μs |    NA | 21.80 |    7 |   16896 B |        9.82 |
| Decrypt_VeryLong_64KB        | 7,902.4 μs |    NA | 22.11 |    8 |  262656 B |      152.71 |
| DecryptOrPlaceholder_NullKey |   404.0 μs |    NA |  1.13 |    3 |         - |        0.00 |
