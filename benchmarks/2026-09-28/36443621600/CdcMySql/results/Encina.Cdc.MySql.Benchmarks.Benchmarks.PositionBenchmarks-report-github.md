```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.560 ns |  0.0427 ns | 0.0023 ns |   0.38 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.405 ns |  0.9807 ns | 0.0538 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   693.828 ns | 81.6547 ns | 4.4758 ns |  73.77 |    0.55 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,093.610 ns | 58.7368 ns | 3.2196 ns | 116.28 |    0.65 | 0.0286 |     504 B |       12.60 |
