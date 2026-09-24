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
| CompareFilePositions |     3.973 ns |  0.0345 ns | 0.0019 ns |   0.39 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |    10.139 ns |  2.6147 ns | 0.1433 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   793.518 ns | 29.8472 ns | 1.6360 ns |  78.27 |    0.97 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,176.812 ns |  6.9230 ns | 0.3795 ns | 116.08 |    1.42 | 0.0286 |     504 B |       12.60 |
