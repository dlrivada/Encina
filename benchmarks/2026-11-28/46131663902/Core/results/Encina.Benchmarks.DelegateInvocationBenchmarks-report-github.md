```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.30 ns |     5.336 ns |   0.292 ns |     1.29 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.76 ns |    10.464 ns |   0.574 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,957.96 ns | 3,523.391 ns | 193.129 ns | 3,091.00 |   52.00 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    132.70 ns |    13.977 ns |   0.766 ns |     4.46 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.47 ns |     6.991 ns |   0.383 ns |     2.81 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
