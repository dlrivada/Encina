```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.173 μs | 1.655 μs | 0.0907 μs |  1.00 |    0.05 |    2 | 0.0076 | 0.0038 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.194 μs | 1.491 μs | 0.0817 μs |  0.55 |    0.04 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.164 μs | 1.398 μs | 0.0766 μs |  0.54 |    0.04 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.167 μs | 1.986 μs | 0.1089 μs |  0.54 |    0.05 |    1 | 0.0038 | 0.0019 |     352 B |        0.37 |
