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
| CompareFilePositions |     3.993 ns |  0.5478 ns | 0.0300 ns |   0.50 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.969 ns |  0.8457 ns | 0.0464 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   762.447 ns | 46.4393 ns | 2.5455 ns |  95.68 |    0.56 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,176.060 ns | 60.8898 ns | 3.3376 ns | 147.59 |    0.83 | 0.0286 |     504 B |       12.60 |
