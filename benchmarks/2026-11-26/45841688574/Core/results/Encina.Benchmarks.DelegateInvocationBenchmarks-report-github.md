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
| CompiledDelegate        |     26.92 ns |     1.124 ns |   0.062 ns |     1.24 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.75 ns |     7.586 ns |   0.416 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,369.49 ns | 3,760.481 ns | 206.125 ns | 2,407.85 |   40.27 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    104.00 ns |    30.447 ns |   1.669 ns |     4.78 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.19 ns |     7.495 ns |   0.411 ns |     3.09 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
