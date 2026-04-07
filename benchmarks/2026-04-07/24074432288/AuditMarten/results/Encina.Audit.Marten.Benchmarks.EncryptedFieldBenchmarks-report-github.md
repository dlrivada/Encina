```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error       | StdDev        | Median         | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|------------:|--------------:|---------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,538.053 ns |  20.5946 ns |    30.1872 ns |   5,543.385 ns |  1.145 |    0.01 |    4 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 142,979.159 ns | 921.5771 ns | 1,321.6985 ns | 143,235.166 ns | 29.566 |    0.28 |    8 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,882.624 ns |  22.0859 ns |    32.3733 ns |   4,863.002 ns |  1.010 |    0.01 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,339.991 ns | 139.9311 ns |   200.6850 ns |   8,345.628 ns |  1.725 |    0.04 |    5 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.227 ns |   0.0017 ns |     0.0024 ns |       1.227 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,145.755 ns |  30.7027 ns |    44.0329 ns |   4,149.654 ns |  0.857 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 122,566.804 ns | 491.2189 ns |   735.2334 ns | 122,359.561 ns | 25.345 |    0.16 |    7 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,607.826 ns |  90.4385 ns |   123.7932 ns |   9,588.738 ns |  1.987 |    0.03 |    6 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,835.994 ns |   9.2078 ns |    13.2056 ns |   4,835.602 ns |  1.000 |    0.00 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
