```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.025 ns |   0.9260 ns | 0.0508 ns |   0.42 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.534 ns |   0.9634 ns | 0.0528 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   795.260 ns | 128.4810 ns | 7.0425 ns |  83.42 |    0.75 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,190.479 ns |  56.8297 ns | 3.1150 ns | 124.87 |    0.66 | 0.0286 |     504 B |       12.60 |
