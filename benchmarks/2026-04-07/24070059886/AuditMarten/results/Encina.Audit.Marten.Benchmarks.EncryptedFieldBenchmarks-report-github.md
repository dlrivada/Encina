```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                       | Mean       | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------- |-----------:|------:|------:|-----:|----------:|------------:|
| Encrypt_Medium_256B          |   334.7 μs |    NA |  0.99 |    1 |    7792 B |        4.53 |
| Encrypt_VeryLong_64KB        |   493.5 μs |    NA |  1.46 |    5 |  569816 B |      331.29 |
| Decrypt_Medium_256B          | 7,199.0 μs |    NA | 21.24 |    7 |    1536 B |        0.89 |
| Decrypt_Long_4KB             | 7,259.1 μs |    NA | 21.41 |    8 |   16896 B |        9.82 |
| DecryptOrPlaceholder_NullKey |   381.9 μs |    NA |  1.13 |    4 |         - |        0.00 |
| Decrypt_Short_16B            | 7,192.8 μs |    NA | 21.22 |    6 |     568 B |        0.33 |
| Decrypt_VeryLong_64KB        | 7,332.7 μs |    NA | 21.63 |    9 |  262656 B |      152.71 |
| Encrypt_Long_4KB             |   335.2 μs |    NA |  0.99 |    2 |   37336 B |       21.71 |
| Encrypt_Short_16B            |   339.0 μs |    NA |  1.00 |    3 |    1720 B |        1.00 |
