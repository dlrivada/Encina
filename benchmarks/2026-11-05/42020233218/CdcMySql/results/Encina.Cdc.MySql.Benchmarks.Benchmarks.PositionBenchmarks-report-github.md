```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.971 ns |  0.0299 ns | 0.0016 ns |   0.43 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.141 ns |  0.4904 ns | 0.0269 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   800.478 ns | 35.5079 ns | 1.9463 ns |  87.57 |    0.29 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,223.362 ns | 61.2910 ns | 3.3596 ns | 133.83 |    0.47 | 0.0286 |     504 B |       12.60 |
