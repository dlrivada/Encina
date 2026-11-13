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
| CompareFilePositions |   2.709 ns |  0.8739 ns | 0.0479 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.911 ns |  2.1726 ns | 0.1191 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 545.680 ns | 54.1706 ns | 2.9693 ns |  78.98 |    1.23 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 854.564 ns | 64.6077 ns | 3.5414 ns | 123.68 |    1.88 | 0.0296 |     504 B |       12.60 |
