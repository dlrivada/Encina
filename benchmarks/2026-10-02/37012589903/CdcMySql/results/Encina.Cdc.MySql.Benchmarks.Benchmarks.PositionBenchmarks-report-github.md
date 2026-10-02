```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.573 ns |  0.1646 ns | 0.0090 ns |   0.38 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.153 ns |  0.1238 ns | 0.0068 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 409.980 ns | 28.1142 ns | 1.5410 ns |  98.71 |    0.35 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 605.481 ns |  6.0311 ns | 0.3306 ns | 145.79 |    0.22 | 0.0296 |     504 B |       12.60 |
