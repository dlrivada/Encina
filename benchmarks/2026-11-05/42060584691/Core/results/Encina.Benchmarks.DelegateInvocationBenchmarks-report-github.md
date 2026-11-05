```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.90 ns |     2.262 ns |   0.124 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.25 ns |     1.605 ns |   0.088 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,280.44 ns | 7,460.524 ns | 408.936 ns | 3,195.25 |   15.22 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    127.48 ns |     7.687 ns |   0.421 ns |     4.51 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     78.01 ns |     7.019 ns |   0.385 ns |     2.76 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
