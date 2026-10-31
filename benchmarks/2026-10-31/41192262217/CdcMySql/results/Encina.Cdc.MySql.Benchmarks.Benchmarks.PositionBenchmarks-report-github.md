```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.982 ns |  0.0858 ns | 0.0047 ns |   0.40 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.853 ns |  2.1119 ns | 0.1158 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   806.368 ns | 21.6479 ns | 1.1866 ns |  81.85 |    0.84 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,160.694 ns | 18.2537 ns | 1.0005 ns | 117.81 |    1.20 | 0.0286 |     504 B |       12.60 |
