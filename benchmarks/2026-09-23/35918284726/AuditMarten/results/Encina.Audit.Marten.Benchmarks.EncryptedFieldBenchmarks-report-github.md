```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,721.380 ns |    272.9478 ns |    14.9612 ns |  1.148 |    0.00 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 146,963.968 ns | 23,365.3801 ns | 1,280.7353 ns | 29.492 |    0.24 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,116.923 ns |    324.5392 ns |    17.7891 ns |  1.027 |    0.00 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,778.749 ns |    965.8166 ns |    52.9397 ns |  1.762 |    0.01 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.232 ns |      0.0961 ns |     0.0053 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,426.261 ns |    221.4569 ns |    12.1388 ns |  0.888 |    0.00 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 123,802.755 ns |  7,437.1408 ns |   407.6548 ns | 24.844 |    0.10 |    4 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,069.766 ns |  1,808.8714 ns |    99.1503 ns |  2.021 |    0.02 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,983.182 ns |    314.6798 ns |    17.2487 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
