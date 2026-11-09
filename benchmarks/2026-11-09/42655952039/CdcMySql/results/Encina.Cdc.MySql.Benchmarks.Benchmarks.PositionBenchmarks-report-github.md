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
| CompareFilePositions |     3.970 ns |  0.0229 ns | 0.0013 ns |   0.49 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.091 ns |  0.7984 ns | 0.0438 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   772.504 ns | 54.5956 ns | 2.9926 ns |  95.48 |    0.55 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,137.027 ns | 20.8637 ns | 1.1436 ns | 140.53 |    0.67 | 0.0286 |     504 B |       12.60 |
