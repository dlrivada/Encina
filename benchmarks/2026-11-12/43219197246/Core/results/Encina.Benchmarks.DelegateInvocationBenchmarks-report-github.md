```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.93 ns |     1.228 ns |   0.067 ns |     1.25 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.53 ns |     0.985 ns |   0.054 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 54,700.06 ns | 5,684.540 ns | 311.589 ns | 2,541.21 |   13.70 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.50 ns |    11.724 ns |   0.643 ns |     4.72 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.28 ns |     0.643 ns |   0.035 ns |     3.03 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
