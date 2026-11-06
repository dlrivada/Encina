```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,417.9 ns | 1,642.6 ns | 90.04 ns |  1.00 |    0.08 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   768.5 ns |   123.1 ns |  6.75 ns |  0.54 |    0.03 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   733.2 ns |   234.0 ns | 12.83 ns |  0.52 |    0.03 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   660.6 ns |   119.8 ns |  6.57 ns |  0.47 |    0.03 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
