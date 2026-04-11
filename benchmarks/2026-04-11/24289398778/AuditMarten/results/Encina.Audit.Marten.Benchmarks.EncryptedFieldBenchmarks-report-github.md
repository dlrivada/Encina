```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,657.789 ns |    28.0570 ns |    40.2385 ns |  0.996 |    0.14 |    4 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 149,963.651 ns | 1,113.9216 ns | 1,597.5531 ns | 26.391 |    3.83 |    8 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,669.637 ns |     5.0347 ns |     6.8916 ns |  0.822 |    0.12 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,665.557 ns |    81.0879 ns |   110.9941 ns |  1.525 |    0.22 |    5 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.057 ns |     0.0012 ns |     0.0017 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,230.932 ns |    77.0869 ns |   105.5174 ns |  0.745 |    0.11 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 129,773.998 ns |   667.9448 ns |   957.9465 ns | 22.838 |    3.31 |    7 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,222.123 ns |    99.2496 ns |   148.5521 ns |  1.799 |    0.26 |    6 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,804.190 ns |   596.8176 ns |   855.9380 ns |  1.021 |    0.21 |    4 |   0.0839 |        - |        - |    1496 B |        1.00 |
