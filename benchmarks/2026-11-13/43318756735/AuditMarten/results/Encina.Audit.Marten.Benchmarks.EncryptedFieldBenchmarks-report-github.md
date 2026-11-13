```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,003.4955 ns |    521.6273 ns |    28.5922 ns |  1.190 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 145,720.1337 ns | 21,860.0379 ns | 1,198.2224 ns | 34.651 |    0.27 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,413.3988 ns |    140.7070 ns |     7.7126 ns |  1.049 |    0.00 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   7,921.5341 ns |  2,025.1763 ns |   111.0067 ns |  1.884 |    0.02 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6391 ns |      0.5299 ns |     0.0290 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,017.2744 ns |     98.2657 ns |     5.3863 ns |  0.955 |    0.00 |    2 |   0.0229 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 173,431.5488 ns | 23,016.4249 ns | 1,261.6079 ns | 41.240 |    0.30 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,880.4562 ns |  3,626.6845 ns |   198.7908 ns |  2.112 |    0.04 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,205.4296 ns |    307.6977 ns |    16.8659 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
