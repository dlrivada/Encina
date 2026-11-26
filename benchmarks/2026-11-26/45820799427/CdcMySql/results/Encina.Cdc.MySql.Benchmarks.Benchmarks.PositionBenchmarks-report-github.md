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
| CompareFilePositions |   2.678 ns |  0.5267 ns | 0.0289 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.811 ns |  1.3543 ns | 0.0742 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 555.233 ns | 80.6507 ns | 4.4207 ns |  81.53 |    0.95 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 832.435 ns | 20.5188 ns | 1.1247 ns | 122.23 |    1.16 | 0.0296 |     504 B |       12.60 |
