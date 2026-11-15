```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.44 ns |   0.137 ns |   0.192 ns |     1.24 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.09 ns |   0.319 ns |   0.458 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,616.80 ns | 248.525 ns | 340.184 ns | 2,427.68 |   51.35 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    105.98 ns |   0.574 ns |   0.804 ns |     4.80 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     71.29 ns |   2.732 ns |   4.004 ns |     3.23 |    0.19 | 0.0105 |      - |     176 B |        1.57 |
