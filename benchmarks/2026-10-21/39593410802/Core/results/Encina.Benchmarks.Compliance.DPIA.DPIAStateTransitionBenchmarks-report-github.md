```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.177 μs | 2.1090 μs | 0.1156 μs |  1.00 |    0.07 |    2 | 0.0076 | 0.0038 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.305 μs | 2.0424 μs | 0.1120 μs |  0.60 |    0.05 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.224 μs | 2.7280 μs | 0.1495 μs |  0.56 |    0.07 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.055 μs | 0.9713 μs | 0.0532 μs |  0.49 |    0.03 |    1 | 0.0038 | 0.0019 |     352 B |        0.37 |
