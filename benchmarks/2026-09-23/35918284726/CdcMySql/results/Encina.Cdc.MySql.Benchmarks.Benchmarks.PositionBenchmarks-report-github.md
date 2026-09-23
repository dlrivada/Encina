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
| CompareFilePositions |     3.979 ns |  0.1467 ns | 0.0080 ns |   0.50 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.965 ns |  1.0966 ns | 0.0601 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   755.269 ns | 80.9479 ns | 4.4370 ns |  94.82 |    0.79 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,174.759 ns | 49.9001 ns | 2.7352 ns | 147.49 |    1.01 | 0.0286 |     504 B |       12.60 |
