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
| CompareFilePositions |     3.969 ns |  0.0480 ns | 0.0026 ns |   0.49 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.076 ns |  0.9596 ns | 0.0526 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   778.395 ns | 71.5602 ns | 3.9225 ns |  96.39 |    0.69 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,158.615 ns | 22.4336 ns | 1.2297 ns | 143.47 |    0.82 | 0.0286 |     504 B |       12.60 |
