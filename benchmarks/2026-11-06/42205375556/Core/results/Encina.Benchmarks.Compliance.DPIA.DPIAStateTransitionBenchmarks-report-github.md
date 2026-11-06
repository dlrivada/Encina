```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,300.4 ns | 297.8 ns | 16.32 ns |  1.00 |    0.02 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   752.5 ns | 463.0 ns | 25.38 ns |  0.58 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   715.5 ns | 397.9 ns | 21.81 ns |  0.55 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   659.1 ns | 209.4 ns | 11.48 ns |  0.51 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
