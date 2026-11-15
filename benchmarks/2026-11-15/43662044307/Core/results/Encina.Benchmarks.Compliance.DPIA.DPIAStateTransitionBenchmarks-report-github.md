```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,879.3 ns | 39.81 ns | 58.35 ns |  1.00 |    0.04 |    3 | 0.0553 | 0.0267 |     976 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,060.0 ns | 26.59 ns | 38.97 ns |  0.56 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        0.41 |
| &#39;Request revision&#39;   | 1,081.7 ns | 28.97 ns | 43.37 ns |  0.58 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        0.41 |
| &#39;Expire assessment&#39;  |   942.2 ns | 13.31 ns | 19.93 ns |  0.50 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.38 |
