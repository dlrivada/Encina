```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,012.0353 ns |    47.8120 ns |    71.5627 ns |  1.121 |    0.03 |    5 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 75,053.7537 ns | 1,024.0795 ns | 1,435.6165 ns | 27.935 |    0.69 |    9 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,545.8839 ns |    50.1935 ns |    70.3642 ns |  0.948 |    0.03 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  4,144.9697 ns |    58.6678 ns |    87.8112 ns |  1.543 |    0.04 |    6 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      0.4859 ns |     0.0426 ns |     0.0638 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,383.7731 ns |    55.4512 ns |    81.2797 ns |  0.887 |    0.03 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 68,591.1135 ns |   663.3569 ns |   951.3667 ns | 25.529 |    0.54 |    8 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  4,633.7889 ns |    51.5161 ns |    73.8829 ns |  1.725 |    0.04 |    7 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,687.4782 ns |    32.0306 ns |    44.9025 ns |  1.000 |    0.02 |    4 |   0.0877 |        - |        - |    1496 B |        1.00 |
