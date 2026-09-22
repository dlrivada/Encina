```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.366 ns |  0.1687 ns | 0.0092 ns |   0.39 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.642 ns |  1.2647 ns | 0.0693 ns |   1.00 |    0.01 | 0.0016 |      40 B |        1.00 |
| FromBytes            |   862.704 ns | 33.6861 ns | 1.8464 ns |  99.83 |    0.72 | 0.0267 |     688 B |       17.20 |
| ToBytes              | 1,203.803 ns | 31.3492 ns | 1.7184 ns | 139.30 |    0.98 | 0.0191 |     504 B |       12.60 |
