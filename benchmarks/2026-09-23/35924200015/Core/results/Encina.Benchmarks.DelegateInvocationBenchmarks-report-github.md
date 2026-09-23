```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     30.04 ns |      1.421 ns |   0.078 ns |     1.24 |    0.04 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     24.25 ns |     15.868 ns |   0.870 ns |     1.00 |    0.04 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 25,207.14 ns | 17,030.456 ns | 933.497 ns | 1,040.27 |   46.03 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     93.70 ns |      3.305 ns |   0.181 ns |     3.87 |    0.12 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     70.47 ns |      5.071 ns |   0.278 ns |     2.91 |    0.09 | 0.0020 |      - |     176 B |        1.57 |
