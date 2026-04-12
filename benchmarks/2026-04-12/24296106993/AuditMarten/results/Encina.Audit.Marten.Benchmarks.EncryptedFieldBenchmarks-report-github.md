```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,447.012 ns |    30.5118 ns |    44.7237 ns |  1.123 |    0.01 |    4 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 141,854.935 ns | 1,120.9950 ns | 1,643.1400 ns | 29.243 |    0.40 |    8 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,774.446 ns |   101.2489 ns |   141.9368 ns |  0.984 |    0.03 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,070.255 ns |    36.9168 ns |    54.1121 ns |  1.664 |    0.02 |    5 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.228 ns |     0.0035 ns |     0.0049 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,272.424 ns |   115.6103 ns |   158.2487 ns |  0.881 |    0.03 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 121,484.650 ns |   568.4101 ns |   850.7696 ns | 25.044 |    0.26 |    7 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,419.407 ns |    39.8902 ns |    57.2093 ns |  1.942 |    0.02 |    6 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,851.216 ns |    27.3545 ns |    38.3471 ns |  1.000 |    0.01 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
