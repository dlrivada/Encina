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
| CompareFilePositions |     3.971 ns |  0.0295 ns | 0.0016 ns |   0.48 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.294 ns |  1.1275 ns | 0.0618 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   795.484 ns | 30.7913 ns | 1.6878 ns |  95.91 |    0.65 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,171.017 ns | 44.9524 ns | 2.4640 ns | 141.19 |    0.95 | 0.0286 |     504 B |       12.60 |
