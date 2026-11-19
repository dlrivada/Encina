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
| CompiledDelegate        |     26.93 ns |     2.037 ns |   0.112 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.49 ns |     1.788 ns |   0.098 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,591.75 ns | 2,853.057 ns | 156.386 ns | 2,446.74 |   11.56 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    102.37 ns |    39.969 ns |   2.191 ns |     4.76 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.44 ns |     6.799 ns |   0.373 ns |     3.04 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
