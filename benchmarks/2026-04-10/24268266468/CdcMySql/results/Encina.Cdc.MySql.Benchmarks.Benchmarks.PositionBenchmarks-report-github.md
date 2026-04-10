```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.989 ns |  0.3262 ns | 0.0179 ns |   0.48 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.342 ns |  1.3231 ns | 0.0725 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   791.356 ns | 59.0850 ns | 3.2386 ns |  94.87 |    0.79 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,172.890 ns |  7.9566 ns | 0.4361 ns | 140.60 |    1.05 | 0.0286 |     504 B |       12.60 |
