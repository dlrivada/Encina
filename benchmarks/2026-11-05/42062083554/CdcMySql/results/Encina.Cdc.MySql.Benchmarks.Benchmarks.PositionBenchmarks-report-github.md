```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.973 ns |  0.1008 ns | 0.0055 ns |   0.48 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.226 ns |  0.8656 ns | 0.0474 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   766.656 ns | 10.4129 ns | 0.5708 ns |  93.20 |    0.47 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,166.297 ns | 63.1852 ns | 3.4634 ns | 141.79 |    0.80 | 0.0286 |     504 B |       12.60 |
