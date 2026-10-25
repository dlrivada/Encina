```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,617.304 ns |    58.5343 ns |    85.7988 ns |  1.154 |    0.02 |    5 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 142,813.341 ns | 1,072.2743 ns | 1,537.8239 ns | 29.342 |    0.35 |    9 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,650.605 ns |    39.1253 ns |    57.3493 ns |  0.956 |    0.01 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,255.236 ns |   111.4217 ns |   166.7707 ns |  1.696 |    0.03 |    6 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.225 ns |     0.0104 ns |     0.0143 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,127.240 ns |    22.7339 ns |    31.1184 ns |  0.848 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 121,776.955 ns |   766.4176 ns | 1,147.1378 ns | 25.020 |    0.27 |    8 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,147.132 ns |   113.9859 ns |   170.6087 ns |  2.085 |    0.04 |    7 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,867.270 ns |    18.4767 ns |    27.0828 ns |  1.000 |    0.01 |    4 |   0.0839 |        - |        - |    1496 B |        1.00 |
