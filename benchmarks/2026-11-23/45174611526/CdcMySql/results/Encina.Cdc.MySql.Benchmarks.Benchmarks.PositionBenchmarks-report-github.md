```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.690 ns |  0.7804 ns | 0.0428 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.864 ns |  0.3155 ns | 0.0173 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 530.014 ns | 35.7055 ns | 1.9571 ns |  77.21 |    0.30 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 826.407 ns | 48.7551 ns | 2.6724 ns | 120.39 |    0.43 | 0.0296 |     504 B |       12.60 |
