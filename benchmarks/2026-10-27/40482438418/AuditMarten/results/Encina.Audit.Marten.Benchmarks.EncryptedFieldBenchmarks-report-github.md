```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error           | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|----------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,222.8309 ns |   3,749.1382 ns |   205.5029 ns |  1.157 |    0.06 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 123,283.5905 ns | 102,918.1425 ns | 5,641.2907 ns | 27.306 |    1.45 |    5 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,482.7846 ns |   2,827.9167 ns |   155.0077 ns |  0.993 |    0.05 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   7,554.7985 ns |   5,432.6729 ns |   297.7831 ns |  1.673 |    0.08 |    4 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.9109 ns |       4.4676 ns |     0.2449 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,626.4508 ns |   2,237.3441 ns |   122.6364 ns |  0.803 |    0.04 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 102,050.2171 ns |  93,693.3360 ns | 5,135.6479 ns | 22.603 |    1.27 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   8,341.7173 ns |   2,910.4173 ns |   159.5298 ns |  1.848 |    0.07 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,519.9103 ns |   3,309.6321 ns |   181.4121 ns |  1.001 |    0.05 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
