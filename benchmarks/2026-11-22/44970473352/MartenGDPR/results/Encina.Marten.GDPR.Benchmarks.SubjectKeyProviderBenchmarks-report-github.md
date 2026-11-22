```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **184.59 ns** |   **1.714 ns** |   **2.513 ns** |  **1.10** |    **0.04** |    **3** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,692.70 ns | 171.150 ns | 245.458 ns | 15.99 |    1.56 |    4 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    73.96 ns |   0.416 ns |   0.622 ns |  0.44 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   168.61 ns |   4.422 ns |   6.619 ns |  1.00 |    0.05 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |            |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **180.10 ns** |   **1.087 ns** |   **1.627 ns** |  **1.00** |    **0.02** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,706.83 ns | 170.039 ns | 238.371 ns | 14.99 |    1.32 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    73.18 ns |   0.681 ns |   0.998 ns |  0.41 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   180.56 ns |   1.873 ns |   2.745 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
