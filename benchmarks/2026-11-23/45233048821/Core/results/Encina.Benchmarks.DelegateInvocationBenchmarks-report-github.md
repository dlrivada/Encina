```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.92 ns |     2.198 ns |   0.120 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.62 ns |     0.490 ns |   0.027 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,457.04 ns | 3,710.287 ns | 203.373 ns | 2,425.79 |    8.55 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.12 ns |     2.427 ns |   0.133 ns |     4.68 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.02 ns |     5.779 ns |   0.317 ns |     3.01 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
