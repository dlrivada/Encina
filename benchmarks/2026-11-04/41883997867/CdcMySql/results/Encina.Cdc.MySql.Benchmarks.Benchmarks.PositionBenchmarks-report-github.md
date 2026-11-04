```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.685 ns |  0.4582 ns | 0.0251 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.923 ns |  2.1537 ns | 0.1181 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 552.052 ns | 59.9660 ns | 3.2869 ns |  79.75 |    1.25 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 844.083 ns | 93.7127 ns | 5.1367 ns | 121.94 |    1.92 | 0.0296 |     504 B |       12.60 |
