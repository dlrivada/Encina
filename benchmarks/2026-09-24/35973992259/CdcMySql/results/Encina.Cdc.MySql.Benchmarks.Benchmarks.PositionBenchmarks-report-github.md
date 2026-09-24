```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |  0.0331 ns | 0.0018 ns |   0.43 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.339 ns |  2.3179 ns | 0.1271 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   799.824 ns | 76.0883 ns | 4.1707 ns |  85.66 |    1.07 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,149.513 ns | 50.8574 ns | 2.7877 ns | 123.11 |    1.46 | 0.0286 |     504 B |       12.60 |
