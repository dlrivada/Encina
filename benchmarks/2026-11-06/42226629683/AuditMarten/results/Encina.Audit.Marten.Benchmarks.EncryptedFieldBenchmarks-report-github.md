```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,060.1082 ns |   173.4725 ns |   9.5086 ns |  1.126 |    0.00 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 144,977.1361 ns | 8,273.1118 ns | 453.4772 ns | 32.273 |    0.10 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,325.0642 ns |   229.5937 ns |  12.5848 ns |  0.963 |    0.00 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   7,740.6178 ns |   797.0886 ns |  43.6911 ns |  1.723 |    0.01 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6016 ns |     0.0282 ns |   0.0015 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,065.7606 ns |   184.6766 ns |  10.1227 ns |  0.905 |    0.00 |    2 |   0.0229 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 168,466.2334 ns | 2,499.8381 ns | 137.0246 ns | 37.502 |    0.06 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,459.0275 ns |   708.0892 ns |  38.8128 ns |  1.883 |    0.01 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,492.2200 ns |   139.9467 ns |   7.6710 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
