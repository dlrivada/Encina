```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.463 ns |  1.154 ns | 0.0633 ns |   0.34 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |    10.326 ns |  2.916 ns | 0.1598 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   714.674 ns | 24.581 ns | 1.3474 ns |  69.22 |    0.93 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,102.075 ns | 36.572 ns | 2.0047 ns | 106.74 |    1.43 | 0.0286 |     504 B |       12.60 |
