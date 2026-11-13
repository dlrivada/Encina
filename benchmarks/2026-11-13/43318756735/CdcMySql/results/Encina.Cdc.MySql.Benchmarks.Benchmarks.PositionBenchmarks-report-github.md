```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.660 ns |  0.1830 ns | 0.0100 ns |   0.37 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.133 ns |  8.2363 ns | 0.4515 ns |   1.00 |    0.08 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 560.333 ns | 73.5064 ns | 4.0291 ns |  78.76 |    4.21 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 847.524 ns | 33.5270 ns | 1.8377 ns | 119.13 |    6.32 | 0.0296 |     504 B |       12.60 |
